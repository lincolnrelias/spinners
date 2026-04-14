using UnityEngine;

public class PoisonPotion : PotionProjectile
{
    [Header("Poison")]
    [SerializeField] private float poisonDuration = 4f;
    [SerializeField] private float poisonDamagePerTick = 5f;
    [SerializeField] private float poisonTickRate = 0.5f;

    protected override void ApplyEffect(SpinnerBase target)
    {
        target.ApplyPoison(poisonDamagePerTick, poisonDuration, poisonTickRate);
    }
}
