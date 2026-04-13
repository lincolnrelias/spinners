using System.Collections.Generic;
using UnityEngine;

public class ExplosivePotion : PotionProjectile
{
    [Header("Explosive")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 40f;

    protected override void ApplyEffect(SpinnerBase target)
    {
        HashSet<SpinnerBase> affected = new HashSet<SpinnerBase>();

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            SpinnerBase spinner = hit.GetComponentInParent<SpinnerBase>();
            if (spinner == null || !affected.Add(spinner)) continue;

            spinner.TakeDamage(explosionDamage);

            Vector3 dir = spinner.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0f)
                spinner.Rb.linearVelocity = dir.normalized * spinner.MoveSpeed;
        }
    }
}
