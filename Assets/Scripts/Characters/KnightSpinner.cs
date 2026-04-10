using UnityEngine;

public class KnightSpinner : SpinnerBase
{
    [Header("Knight - Orbit")]
    [SerializeField] private Transform swordTransform;
    [SerializeField] private Transform shieldTransform;
    [SerializeField] private float orbitSpeed = 120f;

    private Vector3 _swordLocalOffset;
    private Vector3 _shieldLocalOffset;
    private float _currentOrbitAngle;
    private bool _shieldBlockedThisCollision;

    protected override void Awake()
    {
        base.Awake();
        if (swordTransform != null)
            _swordLocalOffset = swordTransform.localPosition;
        if (shieldTransform != null)
            _shieldLocalOffset = shieldTransform.localPosition;
    }

    protected override void UpdateMovement()
    {
        base.UpdateMovement();

        _currentOrbitAngle += orbitSpeed * Time.deltaTime;
        Quaternion rot = Quaternion.Euler(0f, _currentOrbitAngle, 0f);

        if (swordTransform != null)
            swordTransform.localPosition = rot * _swordLocalOffset;
        if (shieldTransform != null)
            shieldTransform.localPosition = rot * _shieldLocalOffset;
    }

    public float GetSwordDamage() => collisionDamage * 1.5f;

    public void NotifyShieldHit() => _shieldBlockedThisCollision = true;

    public void ResetShieldBlock() => _shieldBlockedThisCollision = false;

    public override void TakeDamage(float amount)
    {
        if (_shieldBlockedThisCollision)
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.DebugLogs)
                Debug.Log($"[Shield] {gameObject.name} blocked {amount:F1} incoming damage");
            return;
        }

        base.TakeDamage(amount);
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        if (_shieldBlockedThisCollision)
        {
            // Also prevent the knight from dealing damage to the enemy on this collision.
            // TakeDamage override already covers incoming damage; this covers outgoing.
            return;
        }

        base.OnCollisionEnter(collision);
    }
}
