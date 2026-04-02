using UnityEngine;

public static class Vec2Util
{
    public static float Length(float x, float y)
        => Mathf.Sqrt(x * x + y * y);

    public static void Normalize(float x, float y, out float nx, out float ny)
    {
        float len = Length(x, y);
        if (len < 1e-8f) { nx = 0f; ny = 0f; return; }
        nx = x / len;
        ny = y / len;
    }

    public static float Dot(float ax, float ay, float bx, float by)
        => ax * bx + ay * by;
}
