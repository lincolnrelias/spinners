using UnityEngine;

public abstract class PotionProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem breakVFXPrefab;

    private bool _broken;
    private Rigidbody _rb;

    protected virtual void Awake()
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

    public void Launch(Vector3 origin, Vector3 direction, Collider throwerCollider = null)
    {
        transform.position = origin;

        if (throwerCollider != null)
        {
            foreach (Collider c in GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(c, throwerCollider);
        }

        Vector3 flatDir = new Vector3(direction.x, 0f, direction.z).normalized;
        _rb.linearVelocity = flatDir * moveSpeed;
        _rb.angularVelocity = Random.insideUnitSphere * 8f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_broken) return;

        SpinnerBase hitSpinner = collision.gameObject.GetComponentInParent<SpinnerBase>();
        if (hitSpinner == null) return;

        _broken = true;

        Vector3 contactPoint = collision.GetContact(0).point;

        if (breakVFXPrefab != null)
        {
            ParticleSystem vfx = Instantiate(breakVFXPrefab, contactPoint, Quaternion.identity);
            Destroy(vfx.gameObject, vfx.main.duration);
        }

        bool isBodyHit = collision.gameObject.GetComponent<SpinnerBase>() != null;
        if (isBodyHit)
            ApplyEffect(hitSpinner);

        Destroy(gameObject);
    }

    protected abstract void ApplyEffect(SpinnerBase target);
}
