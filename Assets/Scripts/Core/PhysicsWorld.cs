using System.Collections.Generic;
using UnityEngine;

public class PhysicsWorld : MonoBehaviour
{
    public static PhysicsWorld Instance;

    [Tooltip("Set to true by GameManager after countdown.")]
    public bool IsRunning;

    static readonly List<TopBase> activeTops = new(8);
    readonly SpatialHash spatialHash = new();

    // Tracks pairs currently in contact so spin-transfer + callbacks only fire once per hit
    readonly HashSet<long> _activeContacts = new(4);

    // Shared cooldown for wall-redirect (all agents share one timer)
    static float _wallRedirectCooldown;

    // ── Registration (called from TopBase.OnEnable/OnDisable) ────────────────

    public static void Register(TopBase t)
    {
        if (!activeTops.Contains(t)) activeTops.Add(t);
    }

    public static void Unregister(TopBase t) => activeTops.Remove(t);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        activeTops.Clear();
        _activeContacts.Clear();
        _wallRedirectCooldown = 0f;
    }

    void FixedUpdate()
    {
        if (!IsRunning) return;

        float dt = Time.fixedDeltaTime;

        // — Broad-phase setup —
        spatialHash.Clear();
        for (int i = 0; i < activeTops.Count; i++)
            if (activeTops[i].IsAlive && activeTops[i].IsPhysicsActive)
                spatialHash.Insert(activeTops[i]);

        // — Tick (spin decay + movement) —
        for (int i = 0; i < activeTops.Count; i++)
            activeTops[i].Tick(dt);

        // — Narrow-phase collision (avoid duplicate pairs via Id) —
        for (int i = 0; i < activeTops.Count; i++)
        {
            var a = activeTops[i];
            if (!a.IsAlive || !a.IsPhysicsActive) continue;

            var nearby = spatialHash.Query(a);
            for (int k = 0; k < nearby.Count; k++)
            {
                var b = nearby[k];
                if (b.Id <= a.Id) continue;
                if (!b.IsAlive || !b.IsPhysicsActive) continue;
                ResolveCollision(a, b);
            }
        }

        // — Arena boundary —
        if (_wallRedirectCooldown > 0f) _wallRedirectCooldown -= dt;
        for (int i = 0; i < activeTops.Count; i++)
            if (activeTops[i].IsAlive && activeTops[i].IsPhysicsActive)
                HandleArenaBoundary(activeTops[i]);

        // — Release contact pairs that are fully separated —
        CleanSeparatedContacts();

        // — Systems —
        CameraShake.Instance?.UpdateShake(dt);
    }

    // ── Physics ───────────────────────────────────────────────────────────────

    static long PairKey(TopBase a, TopBase b)
    {
        int lo = a.Id < b.Id ? a.Id : b.Id;
        int hi = a.Id < b.Id ? b.Id : a.Id;
        return ((long)lo << 32) | (uint)hi;
    }

    // Remove pairs whose tops are fully separated so the next touch counts as a new hit
    void CleanSeparatedContacts()
    {
        for (int i = 0; i < activeTops.Count; i++)
        for (int j = i + 1; j < activeTops.Count; j++)
        {
            var a = activeTops[i];
            var b = activeTops[j];
            long key = PairKey(a, b);
            if (!_activeContacts.Contains(key)) continue;

            float dx   = b.X - a.X;
            float dy   = b.Y - a.Y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > a.Radius + b.Radius)   // no longer overlapping
                _activeContacts.Remove(key);
        }
    }

    void ResolveCollision(TopBase a, TopBase b)
    {
        float dx   = b.X - a.X;
        float dy   = b.Y - a.Y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        if (dist >= a.Radius + b.Radius || dist < 0.001f) return;

        long key          = PairKey(a, b);
        bool isNewContact = !_activeContacts.Contains(key);
        _activeContacts.Add(key);

        float nx = dx / dist;
        float ny = dy / dist;

        // Positional separation with a gap buffer so they never sit exactly touching
        float push = (a.Radius + b.Radius - dist) * 0.5f + 1.5f;
        a.X -= nx * push; a.Y -= ny * push;
        b.X += nx * push; b.Y += ny * push;

        float rvx    = b.VX - a.VX;
        float rvy    = b.VY - a.VY;
        float rvDotN = rvx * nx + rvy * ny;
        if (rvDotN > 0f) return; // already separating; positional fix was enough

        // ── Normal impulse (elastic bounce) ──────────────────────────────────
        float e = Mathf.Min(a.Restitution, b.Restitution);
        float j = -(1f + e) * rvDotN / (1f / a.Mass + 1f / b.Mass);

        a.VX -= (j / a.Mass) * nx;  a.VY -= (j / a.Mass) * ny;
        b.VX += (j / b.Mass) * nx;  b.VY += (j / b.Mass) * ny;

        // ── Tangential (spin) impulse ─────────────────────────────────────────
        // Real spinning tops both rotate CCW. Their surfaces move in the tangential
        // direction at the contact point, deflecting each top perpendicular to the
        // collision — A goes one way, B goes the other, creating the classic "swerve".
        float tx = -ny;  // tangential direction: CCW rotation of normal
        float ty =  nx;
        float spinStrength = (a.Spin / a.SpinMax + b.Spin / b.SpinMax) * 0.5f; // 0..1
        float tangImpulse  = Mathf.Abs(j) * spinStrength * 0.28f;

        a.VX -= tx * tangImpulse / a.Mass;  a.VY -= ty * tangImpulse / a.Mass;
        b.VX += tx * tangImpulse / b.Mass;  b.VY += ty * tangImpulse / b.Mass;

        // Preserve direction from impulse but lock speed to BaseSpeed
        SetSpeed(a, a.BaseSpeed);
        SetSpeed(b, b.BaseSpeed);

        // Only on first contact frame: damage, callbacks, FX
        if (!isNewContact) return;

        float spinTransfer = Mathf.Abs(j) * GameConfig.SpinTransferRatio;
        if (a.Spin < b.Spin)
        {
            a.Spin -= spinTransfer;
            b.Spin += spinTransfer * 0.3f;
        }
        else
        {
            b.Spin -= spinTransfer;
            a.Spin += spinTransfer * 0.3f;
        }
        a.Spin = Mathf.Max(a.Spin, 0f);
        b.Spin = Mathf.Max(b.Spin, 0f);

        a.OnCollide(b, Mathf.Abs(j), -nx, -ny);
        b.OnCollide(a, Mathf.Abs(j),  nx,  ny);

        float force = Mathf.Abs(j);
        EventBus.Emit("collision", new CollisionEvent { A = a, B = b, Force = force });

        if (ParticlePool.Instance != null)
        {
            float mx = (a.X + b.X) * 0.5f;
            float my = (a.Y + b.Y) * 0.5f;
            ParticlePool.Instance.EmitImpact(mx, my, a.CharacterColor, b.CharacterColor, force);
        }

        if (CameraShake.Instance != null)
        {
            float mag = force < 50f ? 1f : force < 200f ? 4f : force < 500f ? 8f : 14f;
            CameraShake.Instance.Shake(mag);
        }
    }

    void HandleArenaBoundary(TopBase top)
    {
        float hw = GameConfig.ArenaHalfW - top.Radius;
        float hh = GameConfig.ArenaHalfH - top.Radius;
        bool hit = false;

        // Clamp position and reflect the crossed axis
        if (top.X < -hw) { top.X = -hw; top.VX =  Mathf.Abs(top.VX); hit = true; }
        if (top.X >  hw) { top.X =  hw; top.VX = -Mathf.Abs(top.VX); hit = true; }
        if (top.Y < -hh) { top.Y = -hh; top.VY =  Mathf.Abs(top.VY); hit = true; }
        if (top.Y >  hh) { top.Y =  hh; top.VY = -Mathf.Abs(top.VY); hit = true; }

        if (!hit) return;

        top.Spin -= GameConfig.BorderSpinDrain * top.BaseSpeed;
        top.Spin  = Mathf.Max(top.Spin, 0f);

        // Redirect toward opponent only if it's in the quadrant the spinner is already heading toward
        if (_wallRedirectCooldown <= 0f &&
            top.Opponent != null && top.Opponent.IsAlive)
        {
            float dx   = top.Opponent.X - top.X;
            float dy   = top.Opponent.Y - top.Y;
            float dist = Vec2Util.Length(dx, dy);

            bool inQuadrant = (dx * top.VX >= 0f) && (dy * top.VY >= 0f);

            if (dist > 1f && inQuadrant)
            {
                top.VX = (dx / dist) * top.BaseSpeed;
                top.VY = (dy / dist) * top.BaseSpeed;
                _wallRedirectCooldown = 1f;
            }
            else
            {
                SetSpeed(top, top.BaseSpeed);
            }
        }
        else
        {
            SetSpeed(top, top.BaseSpeed);
        }

        EventBus.Emit("borderHit", new BorderHitEvent { Top = top });
    }

    // Sets a top's speed to the given magnitude, preserving direction.
    static void SetSpeed(TopBase top, float speed)
    {
        float len = Vec2Util.Length(top.VX, top.VY);
        if (len < 0.001f) return;
        float s = speed / len;
        top.VX *= s;
        top.VY *= s;
    }
}
