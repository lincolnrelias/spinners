using UnityEngine;

public class TopBase : MonoBehaviour
{
    // — Identity —
    public int    Id;
    public string CharacterName;
    public Color  CharacterColor;

    // — Position & movement —
    public float X, Y;
    public float VX, VY;
    public float Angle;
    public float Spin;
    public float SpinMax;

    // — Physics —
    public float Radius;
    public float Mass;
    public float Friction;
    public float Restitution;

    // — State —
    public bool  IsAlive;
    public bool  IsPhysicsActive = true;
    public float Wobble;
    public float WobbleAngle;
    public float BaseSpeed;

    // — Combat —
    public TopBase Opponent;

    // — HP shortcuts —
    public float HP      => Spin;
    public float HPMax   => SpinMax;
    public float HPRatio => SpinMax > 0f ? Spin / SpinMax : 0f;

    // — Render components (accessible from subclasses) —
    protected LineRenderer _bodyLR;
    protected LineRenderer _hpRingLR;

    // Pre-allocated HP ring buffer (37 = 36 steps + 1)
    readonly Vector3[] _hpPts = new Vector3[37];

    protected virtual void Awake()
    {
        _bodyLR   = MakeLR("Body",   transform, sortOrder: 1, width: 2f, loop: true);
        _hpRingLR = MakeLR("HPRing", transform, sortOrder: 2, width: 3f, loop: false);
    }

    protected virtual void OnEnable()  => PhysicsWorld.Register(this);
    protected virtual void OnDisable() => PhysicsWorld.Unregister(this);

    // Called by PhysicsWorld in FixedUpdate — pure simulation, no Unity calls
    public void Tick(float dt)
    {
        if (!IsAlive || !IsPhysicsActive) return;

        // Spin decay
        Spin *= (1f - Friction);
        if (Spin < SpinMax * GameConfig.DeathSpinThreshold) { Die(); return; }

        // Position
        X     += VX * dt;
        Y     += VY * dt;
        Angle += Spin * dt;

        // Linear drag (frame-rate independent)
        float drag = Mathf.Pow(GameConfig.LinearDrag, 60f * dt);
        VX *= drag;
        VY *= drag;

        // Wobble
        Wobble       = 1f - HPRatio;
        WobbleAngle += (5f + Wobble * 10f) * dt;

        OnTick(dt);
    }

    // Update runs every frame — rendering only
    void Update()
    {
        if (!IsAlive) return;
        transform.position = new Vector3(X, Y, 0f);
        OnRender();
    }

    // — Hooks —
    public virtual void OnTick(float dt)   { }
    public virtual void OnCollide(TopBase other, float impactForce, float nx, float ny) { }
    public virtual void OnDeath()          { }

    public virtual void OnRender()
    {
        UpdateHPRing();
    }

    void Die()
    {
        IsAlive = false;
        _bodyLR.enabled   = false;
        _hpRingLR.enabled = false;
        OnDeath();
        EventBus.Emit("death", new DeathEvent { Top = this });
    }

    // — Rendering helpers —

    protected void SetBodyPositions(Vector3[] pts)
    {
        _bodyLR.positionCount = pts.Length;
        _bodyLR.SetPositions(pts);
    }

    void UpdateHPRing()
    {
        const int steps = 36;
        int arcPts = Mathf.Max(2, Mathf.RoundToInt(steps * HPRatio));
        _hpRingLR.positionCount = arcPts + 1;

        Color hpColor = HPColor(HPRatio);
        _hpRingLR.startColor = hpColor;
        _hpRingLR.endColor   = hpColor;

        float r = Radius + 5f;
        float startA = Mathf.PI * 0.5f;
        for (int i = 0; i <= arcPts; i++)
        {
            float a = startA + (float)i / steps * Mathf.PI * 2f;
            _hpPts[i] = new Vector3(X + Mathf.Cos(a) * r, Y + Mathf.Sin(a) * r, -0.5f);
        }
        _hpRingLR.SetPositions(_hpPts);
    }

    protected static Color HPColor(float t)
    {
        if (t > 0.5f) return Color.Lerp(Color.yellow, Color.green, (t - 0.5f) * 2f);
        return Color.Lerp(Color.red, Color.yellow, t * 2f);
    }

    protected static LineRenderer MakeLR(string childName, Transform parent, int sortOrder, float width, bool loop)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = loop;
        lr.widthMultiplier = width;
        lr.sortingOrder  = sortOrder;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.numCapVertices    = 0;
        lr.numCornerVertices = 0;

        // Use a vertex-color-aware shader; falls back gracefully if missing
        var shader = Shader.Find("Sprites/Default")
                  ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Unlit/Color");
        lr.material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        return lr;
    }
}
