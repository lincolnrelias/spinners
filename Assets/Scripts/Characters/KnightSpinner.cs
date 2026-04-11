using UnityEngine;

public class KnightSpinner : SpinnerBase
{
    [Header("Knight")]
    [SerializeField] private Transform swordTransform;
    [SerializeField] private Transform shieldTransform;

    private bool _shieldBlockedThisCollision;

    protected override void Awake()
    {
        base.Awake();
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
