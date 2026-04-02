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
/// The manager auto-creates BerserkerTop and ParasitaTop at runtime.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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
        cam.orthographic      = true;
        cam.orthographicSize  = Screen.height * 0.5f; // 1 world unit = 1 pixel
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor   = new Color(0.05f, 0.05f, 0.08f);
    }

    // ── Match flow ────────────────────────────────────────────────────────────

    IEnumerator StartMatch()
    {
        _matchEnded = false;

        // Disable physics while we set up tops
        if (PhysicsWorld.Instance != null)
            PhysicsWorld.Instance.IsRunning = false;

        // — Create Berserker —
        var goA = new GameObject("Berserker");
        _topA   = goA.AddComponent<BerserkerTop>();
        _topA.Id = 0;
        _topA.X  = -GameConfig.SpawnDistance;
        _topA.Y  = 0f;
        _topA.VX = GameConfig.InitialSpeed;
        _topA.VY = 0f;

        // — Create Parasita —
        var goB = new GameObject("Parasita");
        _topB   = goB.AddComponent<ParasitaTop>();
        _topB.Id = 1;
        _topB.X  = GameConfig.SpawnDistance;
        _topB.Y  = 0f;
        _topB.VX = -GameConfig.InitialSpeed;
        _topB.VY = 0f;

        // — Wire opponents —
        _topA.Opponent = _topB;
        _topB.Opponent = _topA;

        // — Countdown —
        HUDController.Instance?.ShowCountdown(3);
        yield return new WaitForSeconds(3.5f);

        // — Start simulation —
        if (PhysicsWorld.Instance != null)
            PhysicsWorld.Instance.IsRunning = true;

        HUDController.Instance?.SetTops(_topA, _topB);
    }

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
        // Phase 1 — freeze physics, max wobble (0.3s)
        loser.IsPhysicsActive = false;
        yield return new WaitForSeconds(0.3f);

        // Phase 2 — spiral collapse (0.8s)
        float elapsed   = 0f;
        float spinStart = loser.Spin;
        while (elapsed < 0.8f)
        {
            elapsed  += Time.deltaTime;
            loser.Spin = Mathf.Lerp(spinStart, 0f, elapsed / 0.8f);
            yield return null;
        }
        loser.Spin = 0f;

        // Phase 3 — explosion
        ParticlePool.Instance?.EmitDeath(loser.X, loser.Y, loser.CharacterColor);
        CameraShake.Instance?.Shake(18f);
        FloatingNumbers.Instance?.Show(loser.X, loser.Y + 50f, "ELIMINADO", Color.white, 24f);

        yield return new WaitForSeconds(1.5f);

        // Phase 4 — result screen
        HUDController.Instance?.ShowResult(winner.CharacterName);
    }
}
