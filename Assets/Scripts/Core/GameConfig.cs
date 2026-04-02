public static class GameConfig
{
    // Arena
    public const float ArenaRadius         = 350f;

    // Physics
    public const float SpinTransferRatio   = 0.015f;
    public const float BorderSpinDrain     = 0.008f;
    public const float LinearDrag          = 0.985f;

    // Spatial Hash
    public const int   CellSize            = 80;

    // Particles
    public const int   ParticlePoolSize    = 512;

    // Camera
    public const float ShakeDecay          = 8f;
    public const float ShakeMax            = 20f;

    // HUD
    public const float HudThrottleFps      = 30f;

    // Death
    public const float DeathSpinThreshold  = 0.02f;

    // Spawn
    public const float SpawnDistance       = 250f;
    public const float InitialSpeed        = 180f;
}
