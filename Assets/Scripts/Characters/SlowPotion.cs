using UnityEngine;

public class SlowPotion : PotionProjectile
{
    [Header("Slow")]
    [SerializeField] private float slowFactor = 0.4f;
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float directDamage = 15f;

    protected override void ApplyEffect(SpinnerBase target)
    {
        target.TakeDamage(directDamage);
        target.ApplySlow(slowFactor, slowDuration);
    }
}
