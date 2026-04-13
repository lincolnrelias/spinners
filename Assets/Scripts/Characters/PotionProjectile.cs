using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PotionType { Explosive, Poison, Slow }

public class PotionProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Colors")]
    [SerializeField] private Color explosiveColor = new Color(1f, 0.35f, 0.05f);
    [SerializeField] private Color poisonColor    = new Color(0.2f, 1f, 0.15f);
    [SerializeField] private Color slowColor      = new Color(0.25f, 0.4f, 1f);

    [Header("VFX")]
    [SerializeField] private ParticleSystem breakVFXPrefab;

    [Header("Explosive")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 40f;

    [Header("Poison")]
    [SerializeField] private float poisonDuration = 4f;
    [SerializeField] private float poisonDamagePerTick = 5f;
    [SerializeField] private float poisonTickRate = 0.5f;

    [Header("Slow")]
    [SerializeField] private float slowFactor = 0.4f;
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float slowDirectDamage = 15f;

    private PotionType _type;
    private bool _broken;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody>();

        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezePositionY
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    private void FixedUpdate()
    {
        Vector3 vel = _rb.linearVelocity;
        vel.y = 0f;
        if (vel.sqrMagnitude > 0f)
            _rb.linearVelocity = vel.normalized * moveSpeed;
    }

    public void Launch(Vector3 origin, Vector3 direction, float speed, PotionType type, Collider throwerCollider = null)
    {
        _type = type;
        moveSpeed = speed;

        transform.position = origin;

        if (throwerCollider != null)
        {
            foreach (Collider c in GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(c, throwerCollider);
        }

        ApplyTint(type);

        if (type == PotionType.Explosive)
            gameObject.AddComponent<KnockbackSource>();

        Vector3 flatDir = new Vector3(direction.x, 0f, direction.z).normalized;
        _rb.linearVelocity = flatDir * moveSpeed;
        _rb.angularVelocity = Random.insideUnitSphere * 8f;
    }

    private void ApplyTint(PotionType type)
    {
        Color tint = type switch
        {
            PotionType.Explosive => explosiveColor,
            PotionType.Poison    => poisonColor,
            PotionType.Slow      => slowColor,
            _                    => Color.white
        };

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", tint);

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.SetPropertyBlock(block);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_broken) return;

        // Only destroy on enemy contact, not walls
        SpinnerBase hitSpinner = collision.gameObject.GetComponentInParent<SpinnerBase>();
        if (hitSpinner == null) return;

        _broken = true;

        Vector3 contactPoint = collision.GetContact(0).point;

        if (breakVFXPrefab != null)
        {
            ParticleSystem vfx = Instantiate(breakVFXPrefab, contactPoint, Quaternion.identity);
            Destroy(vfx.gameObject, vfx.main.duration);
        }

        // Body hit = SpinnerBase lives on the root, weapons/shields are child GameObjects without it
        bool isBodyHit = collision.gameObject.GetComponent<SpinnerBase>() != null;
        if (isBodyHit)
            ApplyEffect(hitSpinner);

        Destroy(gameObject);
    }

    private void ApplyEffect(SpinnerBase target)
    {
        switch (_type)
        {
            case PotionType.Explosive:
                ApplyExplosive();
                break;
            case PotionType.Poison:
                target.ApplyPoison(poisonDamagePerTick, poisonDuration, poisonTickRate);
                break;
            case PotionType.Slow:
                target.TakeDamage(slowDirectDamage);
                target.ApplySlow(slowFactor, slowDuration);
                break;
        }
    }

    private void ApplyExplosive()
    {
        HashSet<SpinnerBase> affected = new HashSet<SpinnerBase>();

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            SpinnerBase spinner = hit.GetComponentInParent<SpinnerBase>();
            if (spinner == null || !affected.Add(spinner)) continue;

            spinner.TakeDamage(explosionDamage);

            // Redirect velocity away from blast center, same speed — mimics a collision
            Vector3 dir = spinner.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0f)
                spinner.Rb.linearVelocity = dir.normalized * spinner.MoveSpeed;
        }
    }
}
