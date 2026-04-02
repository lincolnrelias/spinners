using System.Collections;
using UnityEngine;

/// <summary>
/// Spinning Tops Royale — Game Manager
///
/// SCENE SETUP (do once in Unity):
///   1. Camera: Orthographic, Clear Flags = Solid Color (dark bg), position (0,0,-10)
///   2. GameObject "PhysicsWorld" → add PhysicsWorld.cs + ParticleSystem + ParticlePool.cs
///      ParticleSystem: Stop Action = None, Play On Awake = false
///   3. GameObject "Arena"        → add ArenaController.cs
///   4. GameObject "Systems"      → add CameraShake.cs, FloatingNumbers.cs
///   5. GameObject "GameManager"  → add GameManager.cs (this script)
///   6. GameObject "HUD"          → add HUDController.cs
///   7. Project Settings → Time → Fixed Timestep = 0.01666...
///
/// Assign SpinnerCharacterData assets to the Roster list in the Inspector,
/// then pick Agent A / Agent B by index to choose which two fight.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Roster — arraste os Character Data aqui")]
    [SerializeField] SpinnerCharacterData[] roster;

    [Header("Combate — índices no Roster")]
    [SerializeField] int agentAIndex = 0;
    [SerializeField] int agentBIndex = 1;

    TopBase _topA, _topB;
    bool    _matchEnded;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ConfigureCamera();
        EventBus.Clear();
        EventBus.On("death", OnTopDeath);
        StartCoroutine(StartMatch());
    }

    void ConfigureCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic       = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        // Fit the full arena (ArenaHalfW × ArenaHalfH) with 10 % padding on all sides
        const float padding = 1.1f;
        float fitV = GameConfig.ArenaHalfH * padding;
        float fitH = GameConfig.ArenaHalfW * padding / cam.aspect;
        cam.orthographicSize  = Mathf.Max(fitV, fitH);
        cam.backgroundColor   = new Color(0.05f, 0.05f, 0.08f);
    }

    // ── Match flow ────────────────────────────────────────────────────────────

    IEnumerator StartMatch()
    {
        _matchEnded = false;

        if (PhysicsWorld.Instance != null)
            PhysicsWorld.Instance.IsRunning = false;

        SpinnerCharacterData dataA = GetData(agentAIndex);
        SpinnerCharacterData dataB = GetData(agentBIndex);

        _topA = SpawnTop(dataA, id: 0,
                         x: -GameConfig.SpawnOffsetX, y: -GameConfig.SpawnY,
                         vx:  GameConfig.InitialSpeedX, vy:  GameConfig.InitialSpeedY);

        _topB = SpawnTop(dataB, id: 1,
                         x:  GameConfig.SpawnOffsetX,  y:  GameConfig.SpawnY,
                         vx: -GameConfig.InitialSpeedX, vy: -GameConfig.InitialSpeedY);

        _topA.Opponent = _topB;
        _topB.Opponent = _topA;

        HUDController.Instance?.ShowCountdown(3);
        yield return new WaitForSeconds(3.5f);

        if (PhysicsWorld.Instance != null)
            PhysicsWorld.Instance.IsRunning = true;

        HUDController.Instance?.SetTops(_topA, _topB);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    SpinnerCharacterData GetData(int index)
    {
        if (roster != null && roster.Length > 0)
            return roster[Mathf.Clamp(index, 0, roster.Length - 1)];
        return null;
    }

    TopBase SpawnTop(SpinnerCharacterData data, int id,
                     float x, float y, float vx, float vy)
    {
        string goName = data != null ? data.characterName : "Agent";
        var    go     = new GameObject(goName);

        // Instancia o componente certo baseado no tipo do SO
        TopBase top = data switch
        {
            ParasitaData  => go.AddComponent<ParasitaTop>(),
            BerserkerData => go.AddComponent<BerserkerTop>(),
            _             => go.AddComponent<BerserkerTop>(),
        };

        // Aplica stats do SO (sobrescreve defaults do Awake)
        if (data != null)
        {
            top.CharacterData  = data;
            top.CharacterName  = data.characterName;
            top.CharacterColor = data.characterColor;
            top.Radius         = data.radius;
            top.Mass           = data.mass;
            top.Restitution    = data.restitution;
            top.BaseSpeed      = data.baseSpeed;
            top.SpinMax        = data.spinMax;
            top.Spin           = data.spinMax;
        }

        top.Id = id;
        top.X  = x;  top.Y  = y;
        top.VX = vx; top.VY = vy;

        return top;
    }

    // ── Events ────────────────────────────────────────────────────────────────

    void OnTopDeath(object data)
    {
        if (_matchEnded) return;
        _matchEnded = true;

        var evt    = (DeathEvent)data;
        TopBase loser  = evt.Top;
        TopBase winner = loser == _topA ? _topB : _topA;

        if (PhysicsWorld.Instance != null)
            PhysicsWorld.Instance.IsRunning = false;

        StartCoroutine(DeathSequence(loser, winner));
    }

    IEnumerator DeathSequence(TopBase loser, TopBase winner)
    {
        loser.IsPhysicsActive = false;
        yield return new WaitForSeconds(0.3f);

        float elapsed   = 0f;
        float spinStart = loser.Spin;
        while (elapsed < 0.8f)
        {
            elapsed   += Time.deltaTime;
            loser.Spin = Mathf.Lerp(spinStart, 0f, elapsed / 0.8f);
            yield return null;
        }
        loser.Spin = 0f;

        ParticlePool.Instance?.EmitDeath(loser.X, loser.Y, loser.CharacterColor);
        CameraShake.Instance?.Shake(18f);
        FloatingNumbers.Instance?.Show(loser.X, loser.Y + 50f, "ELIMINADO", Color.white, 24f);

        yield return new WaitForSeconds(1.5f);

        HUDController.Instance?.ShowResult(winner.CharacterName);
    }
}
