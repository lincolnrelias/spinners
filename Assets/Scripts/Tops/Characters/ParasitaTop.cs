using System.Collections.Generic;
using UnityEngine;

public struct Leech
{
    public TopBase Target;
    public float   DrainRate;  // spin/s
    public float   Elapsed;
    public float   Duration;
}

public class ParasitaTop : TopBase
{
    readonly List<Leech> _leeches = new(3);

    // Pre-allocated blob body buffer (8 points)
    readonly Vector3[] _bodyPts = new Vector3[8];

    // Filament + pulse ring LRs (one set per max leech slot)
    LineRenderer[] _filamentLRs;
    LineRenderer[] _pulseRingLRs;
    readonly Vector3[] _ringPts = new Vector3[16]; // 16 steps; loop flag closes it

    protected override void Awake()
    {
        CharacterName  = "Parasita";
        SpinMax        = 400f;
        Spin           = SpinMax;
        Mass           = 0.9f;
        Radius         = 76f;
        Friction       = 0.002f;
        Restitution    = 0.7f;
        CharacterColor = new Color(0.16f, 0.62f, 0.44f);
        BaseSpeed      = 160f;
        IsAlive        = true;

        base.Awake(); // sets up _bodyLR, _hpRingLR

        _filamentLRs  = new LineRenderer[3];
        _pulseRingLRs = new LineRenderer[3];

        Color filColor = new Color(CharacterColor.r, CharacterColor.g, CharacterColor.b, 0.7f);

        for (int i = 0; i < 3; i++)
        {
            var flr = MakeLR($"Filament_{i}",  transform, sortOrder: 3, width: 1.5f, loop: false);
            flr.positionCount = 2;
            flr.startColor = filColor;
            flr.endColor   = filColor;
            flr.enabled    = false;
            _filamentLRs[i] = flr;

            var plr = MakeLR($"PulseRing_{i}", transform, sortOrder: 3, width: 2f,   loop: true);
            plr.positionCount = 16;
            plr.startColor = filColor;
            plr.endColor   = filColor;
            plr.enabled    = false;
            _pulseRingLRs[i] = plr;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // PhysicsWorld.Register
        EventBus.On("borderHit", OnBorderHit);
    }

    protected override void OnDisable()
    {
        base.OnDisable(); // PhysicsWorld.Unregister
        EventBus.Off("borderHit", OnBorderHit);
    }

    void OnBorderHit(object data)
    {
        var evt = (BorderHitEvent)data;
        RemoveLeeches(evt.Top);
    }

    void RemoveLeeches(TopBase target)
    {
        for (int i = _leeches.Count - 1; i >= 0; i--)
            if (_leeches[i].Target == target)
                _leeches.RemoveAt(i);
    }

    ParasitaData PData => CharacterData as ParasitaData;

    public override void OnCollide(TopBase other, float impactForce, float nx, float ny)
    {
        var   pd              = PData;
        int   maxPerTarget    = pd != null ? pd.maxLeechesPerTarget : 3;
        float duration        = pd != null ? pd.leechDuration       : 5f;
        float drainRate       = pd != null ? pd.leechDrainRate      : 0.008f;

        int countOnTarget = 0;
        for (int i = 0; i < _leeches.Count; i++)
            if (_leeches[i].Target == other) countOnTarget++;

        if (countOnTarget >= maxPerTarget) return;

        _leeches.Add(new Leech
        {
            Target    = other,
            DrainRate = other.SpinMax * drainRate,
            Elapsed   = 0f,
            Duration  = duration,
        });

        if (FloatingNumbers.Instance != null)
            FloatingNumbers.Instance.Show(other.X, other.Y, "INFECTADO", CharacterColor, 14f);
    }

    public override void OnTick(float dt)
    {
        float healRatio = PData != null ? PData.leechHealRatio : 0.5f;

        for (int i = _leeches.Count - 1; i >= 0; i--)
        {
            Leech l = _leeches[i];

            if (!l.Target.IsAlive) { _leeches.RemoveAt(i); continue; }

            l.Elapsed += dt;
            if (l.Elapsed >= l.Duration) { _leeches.RemoveAt(i); continue; }

            float drain    = l.DrainRate * dt;
            l.Target.Spin -= drain;
            Spin = Mathf.Min(Spin + drain * healRatio, SpinMax);

            _leeches[i] = l;
        }
    }

    public override void OnRender()
    {
        float tilt  = Mathf.Sin(WobbleAngle) * Wobble * 0.3f;
        float angle = Angle + tilt;

        TopRenderer.ComputeParasitaBody(X, Y, Radius, angle, Time.time, _bodyPts);
        SetBodyPositions(_bodyPts);
        _bodyLR.startColor = CharacterColor;
        _bodyLR.endColor   = CharacterColor;

        UpdateFilaments();

        base.OnRender(); // HP ring
    }

    public override void OnDeath()
    {
        _leeches.Clear();
        HideFilaments();
        if (ParticlePool.Instance != null) ParticlePool.Instance.EmitDeath(X, Y, CharacterColor);
        if (CameraShake.Instance  != null) CameraShake.Instance.Shake(14f);
    }

    // ─────────────────────────────────────────────────────────────

    void UpdateFilaments()
    {
        int active = _leeches.Count;

        for (int i = 0; i < 3; i++)
        {
            if (i < active && _leeches[i].Target != null && _leeches[i].Target.IsAlive)
            {
                var t = _leeches[i].Target;

                // Filament line
                _filamentLRs[i].SetPosition(0, new Vector3(X,    Y,    0f));
                _filamentLRs[i].SetPosition(1, new Vector3(t.X, t.Y,  0f));
                _filamentLRs[i].enabled = true;

                // Pulse ring around infection point
                float pulse = 4f + Mathf.Sin(Time.time * 6f) * 4f; // 0..8
                for (int k = 0; k < 16; k++)
                {
                    float a = (float)k / 16f * Mathf.PI * 2f;
                    _ringPts[k] = new Vector3(t.X + Mathf.Cos(a) * pulse,
                                              t.Y + Mathf.Sin(a) * pulse, 0f);
                }
                _pulseRingLRs[i].positionCount = 16; // loop flag closes the ring
                _pulseRingLRs[i].SetPositions(_ringPts);

                Color pc = new Color(CharacterColor.r, CharacterColor.g, CharacterColor.b, 0.6f);
                _pulseRingLRs[i].startColor = pc;
                _pulseRingLRs[i].endColor   = pc;
                _pulseRingLRs[i].enabled    = true;
            }
            else
            {
                _filamentLRs[i].enabled  = false;
                _pulseRingLRs[i].enabled = false;
            }
        }
    }

    void HideFilaments()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_filamentLRs[i]  != null) _filamentLRs[i].enabled  = false;
            if (_pulseRingLRs[i] != null) _pulseRingLRs[i].enabled = false;
        }
    }
}
