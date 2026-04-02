using UnityEngine;

public class BerserkerTop : TopBase
{
    int _furyStacks;

    // Pre-allocated body buffer: 3 vertices + 3 spike midpoints = 6 points
    readonly Vector3[] _bodyPts = new Vector3[6];
    LineRenderer[] _crackLRs;

    protected override void Awake()
    {
        CharacterName  = "Berserker";
        SpinMax        = 500f;
        Spin           = SpinMax;
        Mass           = 1.0f;
        Radius         = 88f;
        Friction       = 0.0015f;
        Restitution    = 0.8f;
        CharacterColor = new Color(0.91f, 0.35f, 0.24f);
        BaseSpeed      = 180f;
        IsAlive        = true;

        base.Awake(); // sets up _bodyLR, _hpRingLR

        // Crack line renderers (max 5)
        _crackLRs = new LineRenderer[5];
        for (int i = 0; i < 5; i++)
        {
            var lr = MakeLR($"Crack_{i}", transform, sortOrder: 3, width: 1f, loop: false);
            lr.positionCount = 2;
            lr.startColor = new Color(1f, 1f, 1f, 0.7f);
            lr.endColor   = new Color(1f, 1f, 1f, 0f);
            lr.enabled    = false;
            _crackLRs[i]  = lr;
        }
    }

    public override void OnTick(float dt)
    {
        _furyStacks = Mathf.FloorToInt((1f - HPRatio) / 0.1f); // 0..10

        // Fury speed bonus
        var    bData      = CharacterData as BerserkerData;
        float  bonusPerStack = bData != null ? bData.furySpeedBonusPerStack : 0.15f;
        float  speedMult  = 1f + _furyStacks * bonusPerStack;
        float  currentSpeed = Vec2Util.Length(VX, VY);
        float  targetSpeed  = BaseSpeed * speedMult;

        if (currentSpeed > 0.1f && currentSpeed < targetSpeed)
        {
            float scale = targetSpeed / currentSpeed;
            VX *= scale;
            VY *= scale;
        }

        // Sparks when high fury
        if (_furyStacks > 5 && Random.value < 4f * dt && ParticlePool.Instance != null)
            ParticlePool.Instance.EmitSpark(X, Y, CharacterColor, 2f, 40f);
    }

    public override void OnCollide(TopBase other, float impactForce, float nx, float ny)
    {
        if (_furyStacks <= 0) return;
        var   bData        = CharacterData as BerserkerData;
        float transferBonus = bData != null ? bData.furySpinTransferBonusPerStack : 0.08f;
        float bonusDamage   = impactForce * GameConfig.SpinTransferRatio * (_furyStacks * transferBonus);
        other.Spin -= bonusDamage;
        other.Spin  = Mathf.Max(other.Spin, 0f);
    }

    public override void OnRender()
    {
        float visualScale = HPRatio < 0.2f ? 1.1f : 1.0f;
        float r           = Radius * visualScale;
        float tilt        = Mathf.Sin(WobbleAngle) * Wobble * 0.3f;
        float angle       = Angle + tilt;
        float spikeScale  = 0.15f + _furyStacks * 0.02f;

        TopRenderer.ComputeBerserkerBody(X, Y, r, angle, spikeScale, _bodyPts);
        SetBodyPositions(_bodyPts);

        // Colour: saturate toward red at low HP
        Color bodyColor = CharacterColor;
        if (HPRatio < 0.5f)
        {
            float t = 1f - HPRatio * 2f;
            bodyColor = Color.Lerp(CharacterColor, Color.red, t * 0.6f);
        }
        _bodyLR.startColor = bodyColor;
        _bodyLR.endColor   = bodyColor;

        UpdateCracks(angle);

        base.OnRender(); // HP ring
    }

    public override void OnDeath()
    {
        HideCracks();
        if (ParticlePool.Instance != null) ParticlePool.Instance.EmitDeath(X, Y, CharacterColor);
        if (CameraShake.Instance  != null) CameraShake.Instance.Shake(18f);
    }

    // ─────────────────────────────────────────────────────────────

    void UpdateCracks(float angle)
    {
        float crackIntensity = 1f - HPRatio;
        int   cracks         = Mathf.FloorToInt(crackIntensity * 5f);

        for (int i = 0; i < 5; i++)
        {
            if (i < cracks)
            {
                float a   = (float)i / 5f * Mathf.PI * 2f + angle;
                float len = Radius * 0.6f * crackIntensity;
                _crackLRs[i].SetPosition(0, new Vector3(X, Y, 0f));
                _crackLRs[i].SetPosition(1, new Vector3(X + Mathf.Cos(a) * len, Y + Mathf.Sin(a) * len, 0f));
                _crackLRs[i].enabled = true;
            }
            else
            {
                _crackLRs[i].enabled = false;
            }
        }
    }

    void HideCracks()
    {
        if (_crackLRs == null) return;
        for (int i = 0; i < _crackLRs.Length; i++)
            if (_crackLRs[i] != null) _crackLRs[i].enabled = false;
    }
}
