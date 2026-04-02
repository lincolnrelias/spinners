using System.Collections.Generic;
using UnityEngine;

public class PhysicsWorld : MonoBehaviour
{
    public static PhysicsWorld Instance;

    [Tooltip("Set to true by GameManager after countdown.")]
    public bool IsRunning;

    static readonly List<TopBase> activeTops = new(8);
    readonly SpatialHash spatialHash = new();

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
        for (int i = 0; i < activeTops.Count; i++)
            if (activeTops[i].IsAlive && activeTops[i].IsPhysicsActive)
                HandleArenaBoundary(activeTops[i]);

        // — Steering: ensure tops always chase each other —
        for (int i = 0; i < activeTops.Count; i++)
            SteerTop(activeTops[i], dt);

        // — Systems —
        CameraShake.Instance?.UpdateShake(dt);
    }

    // ── Physics ───────────────────────────────────────────────────────────────

    void ResolveCollision(TopBase a, TopBase b)
    {
        float dx   = b.X - a.X;
        float dy   = b.Y - a.Y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        if (dist >= a.Radius + b.Radius || dist < 0.001f) return;

        float nx = dx / dist;
        float ny = dy / dist;

        // Positional separation
        float overlap = (a.Radius + b.Radius - dist) * 0.5f;
        a.X -= nx * overlap; a.Y -= ny * overlap;
        b.X += nx * overlap; b.Y += ny * overlap;

        // Velocity impulse
        float rvx    = b.VX - a.VX;
        float rvy    = b.VY - a.VY;
        float rvDotN = rvx * nx + rvy * ny;
        if (rvDotN > 0f) return;  // already separating

        float e = Mathf.Min(a.Restitution, b.Restitution);
        float j = -(1f + e) * rvDotN / (1f / a.Mass + 1f / b.Mass);

        a.VX -= (j / a.Mass) * nx; a.VY -= (j / a.Mass) * ny;
        b.VX += (j / b.Mass) * nx; b.VY += (j / b.Mass) * ny;

        // Spin transfer (weaker top loses spin)
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

        // Particles
        if (ParticlePool.Instance != null)
        {
            float mx = (a.X + b.X) * 0.5f;
            float my = (a.Y + b.Y) * 0.5f;
            ParticlePool.Instance.EmitImpact(mx, my, a.CharacterColor, b.CharacterColor, force);
        }

        // Camera shake calibrated by force
        if (CameraShake.Instance != null)
        {
            float mag = force < 50f ? 1f : force < 200f ? 4f : force < 500f ? 8f : 14f;
            CameraShake.Instance.Shake(mag);
        }
    }

    void HandleArenaBoundary(TopBase top)
    {
        float dx   = top.X;
        float dy   = top.Y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        float limit = GameConfig.ArenaRadius - top.Radius;

        if (dist <= limit) return;

        float nx = dx / dist;
        float ny = dy / dist;
        top.X = nx * limit;
        top.Y = ny * limit;

        float dot = top.VX * nx + top.VY * ny;
        top.VX -= 2f * dot * nx * top.Restitution;
        top.VY -= 2f * dot * ny * top.Restitution;
        top.Spin -= Mathf.Abs(dot) * GameConfig.BorderSpinDrain;

        EventBus.Emit("borderHit", new BorderHitEvent { Top = top });
    }

    // Fallback steering: if a top slows too much, push it toward its opponent
    void SteerTop(TopBase top, float dt)
    {
        if (!top.IsAlive || !top.IsPhysicsActive) return;
        if (top.Opponent == null || !top.Opponent.IsAlive) return;

        float speed = Vec2Util.Length(top.VX, top.VY);
        if (speed >= top.BaseSpeed * 0.35f) return;

        float dx   = top.Opponent.X - top.X;
        float dy   = top.Opponent.Y - top.Y;
        float dist = Vec2Util.Length(dx, dy);
        if (dist < 1f) return;

        top.VX = (dx / dist) * top.BaseSpeed * 0.7f;
        top.VY = (dy / dist) * top.BaseSpeed * 0.7f;
    }
}
