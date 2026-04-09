using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpinnerBase : MonoBehaviour
{
    [SerializeField] protected float spinSpeed = 180f;
    [SerializeField] protected float moveSpeed = 5f;

    [Header("Health")]
    [SerializeField] private float startingHealth = 100f;
    [SerializeField] private float collisionDamage = 25f;

    public float StartingHealth => startingHealth;

    [Header("Audio")]
    [SerializeField] private AudioClip spinnerCollisionClip;
    [SerializeField] private AudioClip wallCollisionClip;

    [Header("VFX")]
    [SerializeField] private ParticleSystem collisionVFXPrefab;

    public ParticleSystem CollisionVFXPrefab => collisionVFXPrefab;

    protected Rigidbody rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;

        InitMovement();
    }

    protected virtual void Update()
    {
        UpdateMovement();
    }

    protected virtual void FixedUpdate()
    {
        FixedUpdateMovement();
    }

    protected virtual void InitMovement()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        rb.linearVelocity = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * moveSpeed;
    }

    protected virtual void UpdateMovement()
    {
        transform.Rotate(Vector3.up * (spinSpeed * Time.deltaTime), Space.World);
    }

    protected virtual void FixedUpdateMovement()
    {
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel.normalized * moveSpeed;
    }

    public virtual void TakeDamage(float amount)
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.RegisterDamage(this, amount);
    }

    public virtual void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        Destroy(gameObject);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        SpinnerBase otherSpinner = collision.gameObject.GetComponent<SpinnerBase>();

        if (otherSpinner != null)
        {
            otherSpinner.TakeDamage(collisionDamage);
            if (CollisionSoundManager.Instance != null)
                CollisionSoundManager.Instance.PlaySpinnerCollision(spinnerCollisionClip);
            if (GetEntityId() > otherSpinner.GetEntityId())
            {
                ParticleSystem chosenPrefab = Random.value < 0.5f ? collisionVFXPrefab : otherSpinner.CollisionVFXPrefab;
                if (chosenPrefab != null)
                {
                    Vector3 contactPoint = collision.GetContact(0).point;
                    ParticleSystem vfx = Instantiate(chosenPrefab, contactPoint, Quaternion.identity);
                    Destroy(vfx.gameObject, vfx.main.duration);
                }
            }
        }
        else
        {
            if (CollisionSoundManager.Instance != null)
                CollisionSoundManager.Instance.PlayWallCollision(wallCollisionClip);
        }
    }
}
