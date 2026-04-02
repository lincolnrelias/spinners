using UnityEngine;

/// <summary>
/// Renders the rectangular arena boundary (portrait / 9:16 reels format).
/// Attach to any GameObject in the scene.
/// </summary>
public class ArenaController : MonoBehaviour
{
    public static ArenaController Instance;

    void Awake()
    {
        Instance = this;
        BuildArena();
    }

    void BuildArena()
    {
        float hw = GameConfig.ArenaHalfW;
        float hh = GameConfig.ArenaHalfH;

        // — Main border —
        MakeRect("ArenaBorder",
            hw, hh,
            color:     new Color(0.88f, 0.88f, 1.00f, 0.95f),
            width:     5f,
            sortOrder: -1);

        // — Inner glow (inset by 10 units) —
        MakeRect("ArenaGlow",
            hw - 10f, hh - 10f,
            color:     new Color(0.45f, 0.45f, 1.00f, 0.18f),
            width:     14f,
            sortOrder: -2);

        // — Corner brackets —
        const float arm = 60f; // length of each bracket arm
        MakeCornerBracket("CornerBL", -hw, -hh,  1f,  1f, arm);
        MakeCornerBracket("CornerBR",  hw, -hh, -1f,  1f, arm);
        MakeCornerBracket("CornerTR",  hw,  hh, -1f, -1f, arm);
        MakeCornerBracket("CornerTL", -hw,  hh,  1f, -1f, arm);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void MakeRect(string id, float hw, float hh, Color color, float width, int sortOrder)
    {
        var go = new GameObject(id);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace   = true;
        lr.loop            = true;
        lr.positionCount   = 4;
        lr.widthMultiplier = width;
        lr.sortingOrder    = sortOrder;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.numCapVertices    = 2;
        lr.material = MakeMat();
        lr.startColor = color;
        lr.endColor   = color;
        lr.SetPositions(new Vector3[]
        {
            new(-hw, -hh, 0f),
            new( hw, -hh, 0f),
            new( hw,  hh, 0f),
            new(-hw,  hh, 0f),
        });
    }

    // L-shaped corner bracket: cornerX/Y is the corner position, signX/Y point inward
    void MakeCornerBracket(string id, float cx, float cy, float sx, float sy, float arm)
    {
        var go = new GameObject(id);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace   = true;
        lr.loop            = false;
        lr.positionCount   = 3;
        lr.widthMultiplier = 5f;
        lr.sortingOrder    = 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.numCapVertices    = 2;
        lr.material = MakeMat();
        lr.startColor = new Color(0.88f, 0.88f, 1.00f, 0.95f);
        lr.endColor   = new Color(0.88f, 0.88f, 1.00f, 0.95f);
        // Arm along X, then corner, then arm along Y
        lr.SetPositions(new Vector3[]
        {
            new(cx + sx * arm, cy,           0f),
            new(cx,            cy,           0f),
            new(cx,            cy + sy * arm, 0f),
        });
    }

    static Material MakeMat()
    {
        var shader = Shader.Find("Sprites/Default")
                  ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Unlit/Color");
        return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
    }
}
