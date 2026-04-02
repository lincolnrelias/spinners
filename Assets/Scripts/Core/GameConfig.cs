public static class GameConfig
{
    // Arena — rectangle sized for 9:16 reels (portrait)
    public const float ArenaHalfW        = 500f;   // total width  1000 px
    public const float ArenaHalfH        = 860f;   // total height 1720 px

    // Physics
    public const float SpinTransferRatio = 0.015f;
    public const float BorderSpinDrain   = 0.008f;
    public const float LinearDrag        = 0.985f;

    // Spatial Hash
    public const int   CellSize          = 80;

    // Particles
    public const int   ParticlePoolSize  = 512;

    // Camera
    public const float ShakeDecay        = 8f;
    public const float ShakeMax          = 20f;

    // HUD
    public const float HudThrottleFps   = 30f;

    // Death
    public const float DeathSpinThreshold = 0.02f;

    // Spawn — tops enter from opposite vertical thirds, slight diagonal
    public const float SpawnY            = 560f;   // |Y| offset from center
    public const float SpawnOffsetX      = 90f;    // horizontal stagger
    public const float InitialSpeedY     = 165f;   // main vertical component
    public const float InitialSpeedX     = 55f;    // slight diagonal component
}
