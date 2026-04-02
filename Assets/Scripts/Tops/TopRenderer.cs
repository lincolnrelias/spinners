using UnityEngine;

/// <summary>
/// Static helpers for computing procedural shape vertex positions.
/// Called by character OnRender() overrides.
/// </summary>
public static class TopRenderer
{
    /// <summary>
    /// Fills outPts (length 6) with Berserker triangle + spike vertices in world space.
    /// </summary>
    public static void ComputeBerserkerBody(
        float cx, float cy, float radius, float angle, float spikeScale,
        Vector3[] outPts)
    {
        const int sides = 3;
        float step  = Mathf.PI * 2f / sides;
        float spikeR = radius * (1f + spikeScale);

        for (int i = 0; i < sides; i++)
        {
            float v = angle + i * step;
            float m = angle + (i + 0.5f) * step;
            outPts[i * 2]     = new Vector3(cx + Mathf.Cos(v) * radius, cy + Mathf.Sin(v) * radius, 0f);
            outPts[i * 2 + 1] = new Vector3(cx + Mathf.Cos(m) * spikeR, cy + Mathf.Sin(m) * spikeR, 0f);
        }
    }

    /// <summary>
    /// Fills outPts (length 8) with Parasita organic blob vertices in world space.
    /// </summary>
    public static void ComputeParasitaBody(
        float cx, float cy, float radius, float angle, float time,
        Vector3[] outPts)
    {
        const int sides = 8;
        float step = Mathf.PI * 2f / sides;

        for (int i = 0; i < sides; i++)
        {
            float a      = angle + i * step;
            float wobble = 1f + Mathf.Sin(time * 2f + i * 1.3f) * 0.08f;
            outPts[i] = new Vector3(cx + Mathf.Cos(a) * radius * wobble,
                                    cy + Mathf.Sin(a) * radius * wobble, 0f);
        }
    }

    /// <summary>
    /// Fills outPts with HP arc points; returns the count written.
    /// outPts must have at least 37 elements.
    /// </summary>
    public static int ComputeHPArc(
        float cx, float cy, float radius, float hpRatio,
        Vector3[] outPts)
    {
        const int steps = 36;
        int count = Mathf.Max(2, Mathf.RoundToInt(steps * hpRatio) + 1);
        float startA = Mathf.PI * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float a = startA + (float)i / steps * Mathf.PI * 2f;
            outPts[i] = new Vector3(cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius, -0.5f);
        }
        return count;
    }
}
