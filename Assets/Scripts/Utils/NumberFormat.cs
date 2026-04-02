using UnityEngine;

public static class NumberFormat
{
    public static string Fmt(float n)
    {
        if (n >= 1e12f) return $"{n / 1e12f:F1}T";
        if (n >= 1e9f)  return $"{n / 1e9f:F1}B";
        if (n >= 1e6f)  return $"{n / 1e6f:F1}M";
        if (n >= 1e3f)  return $"{n / 1e3f:F1}K";
        return Mathf.RoundToInt(n).ToString();
    }
}
