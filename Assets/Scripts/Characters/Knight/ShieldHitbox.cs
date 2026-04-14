using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class ShieldHitbox : MonoBehaviour
{
    [SerializeField] private KnightSpinner knight;
    [SerializeField] private float hitCooldown = 0.3f;

    [Header("Effects")]
    [SerializeField] private AudioClip blockClip;
    [SerializeField] private ParticleSystem blockVFXPrefab;

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

        // Flag the knight so TakeDamage and OnCollisionEnter are both blocked this hit
        knight.NotifyShieldHit();

        if (GameStateManager.Instance != null && GameStateManager.Instance.DebugLogs)
            Debug.Log($"[Shield] {knight.gameObject.name} blocked hit from {enemy.gameObject.name} — no damage dealt or received");

        // Apply bounce — enemy deflects off shield as if it hit the knight's body
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
        if (blockClip != null)
            _audioSource.PlayOneShot(blockClip);

        if (blockVFXPrefab != null)
        {
            Vector3 contactPoint = other.ClosestPoint(transform.position);
            ParticleSystem vfx = Instantiate(blockVFXPrefab, contactPoint, Quaternion.identity);
            Destroy(vfx.gameObject, vfx.main.duration);
        }
    }

    private IEnumerator TemporarilyIgnoreCollision(Collider a, Collider b, float duration)
    {
        if (a == null || b == null) yield break;
        Physics.IgnoreCollision(a, b, true);
        yield return new WaitForSeconds(duration);
        // Reset the shield flag before re-enabling collision so it doesn't bleed
        // into the next unrelated hit (Physics.IgnoreCollision prevented the main body
        // from ever firing OnCollisionEnter, so the flag would otherwise stay sticky).
        knight.ResetShieldBlock();
        if (a != null && b != null)
            Physics.IgnoreCollision(a, b, false);
    }
}
