using UnityEngine;

// Attach to any object that should redirect a SpinnerBase's velocity on collision.
// Layer and tag filters mirror the Include/Exclude pattern on Unity colliders:
//   - Include Layers / Include Tags: target must match (empty = everything passes)
//   - Exclude Layers / Exclude Tags: target must NOT match (empty = nothing blocked)
// Both checks are AND'd: a target must pass layers AND tags to receive knockback.
public class KnockbackSource : MonoBehaviour
{
    [Header("Layer Filter")]
    [Tooltip("Knockback is applied only to targets on these layers. Default: Everything.")]
    [SerializeField] private LayerMask includeLayers = ~0;
    [Tooltip("Knockback is never applied to targets on these layers. Default: Nothing.")]
    [SerializeField] private LayerMask excludeLayers = 0;

    [Header("Tag Filter")]
    [Tooltip("If non-empty, target must match one of these tags.")]
    [TagSelector] [SerializeField] private string[] includeTags = {};
    [Tooltip("If non-empty, target must NOT match any of these tags.")]
    [TagSelector] [SerializeField] private string[] excludeTags = {};

    public bool Affects(GameObject target)
    {
        int mask = 1 << target.layer;

        if ((mask & includeLayers) == 0) return false;
        if ((mask & excludeLayers) != 0) return false;

        if (includeTags.Length > 0)
        {
            bool found = false;
            foreach (string t in includeTags)
                if (target.CompareTag(t)) { found = true; break; }
            if (!found) return false;
        }

        if (excludeTags.Length > 0)
            foreach (string t in excludeTags)
                if (target.CompareTag(t)) return false;

        return true;
    }
}
