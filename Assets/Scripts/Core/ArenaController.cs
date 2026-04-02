using UnityEngine;

/// <summary>
/// Renders the circular arena boundary and floor.
/// Attach to any GameObject in the scene.
/// </summary>
public class ArenaController : MonoBehaviour
{
    public static ArenaController Instance;

    const int CircleSteps = 64;

    LineRenderer _borderLR;
    LineRenderer _innerGlowLR;

    void Awake()
    {
        Instance = this;
        BuildArena();
    }

    void BuildArena()
    {
        // Outer border
        _borderLR = MakeCircleLR("ArenaBorder",
            radius: GameConfig.ArenaRadius,
            color:  new Color(0.85f, 0.85f, 1.0f, 0.9f),
            width:  5f,
            sortOrder: -1);

        // Subtle inner glow ring slightly smaller
        _innerGlowLR = MakeCircleLR("ArenaGlow",
            radius: GameConfig.ArenaRadius - 12f,
            color:  new Color(0.5f, 0.5f, 1.0f, 0.2f),
            width:  12f,
            sortOrder: -2);
    }

    LineRenderer MakeCircleLR(string childName, float radius, Color color, float width, int sortOrder)
    {
        var go  = new GameObject(childName);
        go.transform.SetParent(transform, false);
        var lr  = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = CircleSteps;
        lr.widthMultiplier = width;
        lr.sortingOrder    = sortOrder;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;

        var shader = Shader.Find("Sprites/Default")
                  ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Unlit/Color");
        lr.material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        lr.startColor = color;
        lr.endColor   = color;

        var pts = new Vector3[CircleSteps];
        for (int i = 0; i < CircleSteps; i++)
        {
            float a = (float)i / CircleSteps * Mathf.PI * 2f;
            pts[i] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
        }
        lr.SetPositions(pts);
        return lr;
    }
}
