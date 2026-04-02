using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticlePool : MonoBehaviour
{
    public static ParticlePool Instance;

    ParticleSystem _ps;
    ParticleSystem.EmitParams _ep;

    void Awake()
    {
        Instance = this;
        _ps = GetComponent<ParticleSystem>();

        var main = _ps.main;
        main.loop            = false;
        main.maxParticles    = GameConfig.ParticlePoolSize;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize       = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSpeed      = 0f;
        main.gravityModifier = 0f;

        var emission = _ps.emission;
        emission.enabled = false;

        var shape = _ps.shape;
        shape.enabled = false;
    }

    public void EmitSpark(float x, float y, Color color, float radius = 3f, float speed = 80f)
    {
        _ep.position      = new Vector3(x, y, 0f);
        Vector2 v2        = Random.insideUnitCircle * speed;
        _ep.velocity      = new Vector3(v2.x, v2.y, 0f);
        _ep.startSize     = radius * 2f;
        _ep.startColor    = color;
        _ep.startLifetime = 0.4f;
        _ps.Emit(_ep, 1);
    }

    public void EmitImpact(float x, float y, Color colorA, Color colorB, float force)
    {
        int sparks = force < 50f ? 4 : force < 200f ? 8 : 12;
        Color c = Color.Lerp(colorA, colorB, 0.5f);
        for (int i = 0; i < sparks; i++)
            EmitSpark(x, y, c, 2.5f, force * 0.5f);
        if (force >= 200f)
            for (int i = 0; i < 4; i++)
                EmitSpark(x, y, Color.white, 1.5f, force * 0.3f);
    }

    public void EmitDeath(float x, float y, Color color)
    {
        for (int i = 0; i < 20; i++) EmitSpark(x, y, color, 4f, 120f);
        for (int i = 0; i < 5;  i++) EmitSpark(x, y, Color.white, 2f, 80f);
    }
}
