using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class SwordHitbox : MonoBehaviour
{
    [SerializeField] private KnightSpinner knight;
    [SerializeField] private float hitCooldown = 0.3f;

    [Header("Effects")]
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private ParticleSystem hitVFXPrefab;

    private AudioSource _audioSource;
    private readonly Dictionary<SpinnerBase, float> _hitTimestamps = new();

    private void Awake()
    {
        if (knight == null)
            knight = GetComponentInParent<KnightSpinner>();

        GetComponent<Collider>().isTrigger = true;
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        SpinnerBase enemy = other.GetComponent<SpinnerBase>()
                         ?? other.GetComponentInParent<SpinnerBase>();

        if (enemy == null || enemy == (SpinnerBase)knight) return;

        if (_hitTimestamps.TryGetValue(enemy, out float lastHit) && Time.time - lastHit < hitCooldown)
            return;

        _hitTimestamps[enemy] = Time.time;

        float damage = knight.GetSwordDamage();
        enemy.TakeDamage(damage);

        if (GameStateManager.Instance != null && GameStateManager.Instance.DebugLogs)
            Debug.Log($"[Sword] {knight.gameObject.name} hit {enemy.gameObject.name} for {damage:F1} damage (1.5x)");

        ApplyBounceImpulse(enemy);
        PlayEffects(other);

        // Briefly prevent the knight's main body from re-colliding with the same target
        StartCoroutine(TemporarilyIgnoreCollision(
            knight.GetComponent<Collider>(),
            other,
            hitCooldown));
    }

    private void ApplyBounceImpulse(SpinnerBase enemy)
    {
        if (knight.Rb == null) return;

        Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
        if (enemyRb == null) return;

        Vector3 dir = enemy.transform.position - knight.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;
        dir.Normalize();

        enemyRb.linearVelocity = dir * enemy.MoveSpeed;
        knight.Rb.linearVelocity = -dir * knight.MoveSpeed;
    }

    private void PlayEffects(Collider other)
    {
        if (hitClip != null)
            _audioSource.PlayOneShot(hitClip);

        if (hitVFXPrefab != null)
        {
            Vector3 contactPoint = other.ClosestPoint(transform.position);
            ParticleSystem vfx = Instantiate(hitVFXPrefab, contactPoint, Quaternion.identity);
            Destroy(vfx.gameObject, vfx.main.duration);
        }
    }

    private IEnumerator TemporarilyIgnoreCollision(Collider a, Collider b, float duration)
    {
        if (a == null || b == null) yield break;
        Physics.IgnoreCollision(a, b, true);
        yield return new WaitForSeconds(duration);
        if (a != null && b != null)
            Physics.IgnoreCollision(a, b, false);
    }
}
