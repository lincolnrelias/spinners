================================================================================
  SPINNING TOPS ROYALE — UNITY SPEC
  Versão 1.0 (Berserker + Parasita) | C# / Unity 2022.3 LTS / URP
================================================================================

--------------------------------------------------------------------------------
  VISÃO GERAL
--------------------------------------------------------------------------------

Jogo de arena autônoma com física de peões giratórios. Dois peões combatem dentro
de uma arena circular. Simulação "assistida": o jogador apenas assiste a batalha.
Resultado determinado inteiramente por física e habilidades — sem RNG puro além
do spawn.

Stack: Unity 2022.3 LTS, URP, C#.
Plataforma alvo: PC/Mac (gravação de tela para shorts). Canvas 2D (Camera ortográfica).
Sem assets de terceiros. Tudo renderizado proceduralmente via código.

Resolução alvo: 1080×1920 (vertical) ou 1280×720. Configurável via constante.


--------------------------------------------------------------------------------
  ESTRUTURA DE PASTAS (Assets/)
--------------------------------------------------------------------------------

Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs          ← estado global, fluxo de partida
│   │   ├── ArenaController.cs      ← boundary logic + renderização da arena
│   │   ├── PhysicsWorld.cs         ← resolução de colisão custom + spatial hash
│   │   └── EventBus.cs             ← sistema de eventos desacoplado
│   ├── Tops/
│   │   ├── TopBase.cs              ← classe base com todos os campos e hooks
│   │   ├── TopRenderer.cs          ← renderização procedural (LineRenderer/GL)
│   │   └── Characters/
│   │       ├── BerserkerTop.cs
│   │       └── ParasitaTop.cs
│   ├── Systems/
│   │   ├── ParticlePool.cs         ← pool de partículas manual
│   │   ├── SpatialHash.cs          ← grid hash para broad-phase
│   │   ├── CameraShake.cs          ← screen shake
│   │   └── FloatingNumbers.cs      ← dano flutuante
│   ├── HUD/
│   │   └── HUDController.cs        ← barras de HP, log de eventos
│   └── Utils/
│       ├── Vec2Util.cs             ← operações vetoriais helpers
│       └── NumberFormat.cs         ← fmt() para números grandes
├── Prefabs/
│   ├── TopBase.prefab
│   ├── ArenaController.prefab
│   └── HUD.prefab
├── Scenes/
│   └── Arena.unity
└── Settings/
    └── GameConfig.cs               ← todas as constantes em um lugar só


--------------------------------------------------------------------------------
  GAME LOOP — FIXEDUPDATE + INTERPOLAÇÃO
--------------------------------------------------------------------------------

Unity já provê Fixed Timestep via FixedUpdate(). Configure:

  Project Settings → Time → Fixed Timestep = 0.01666... (1/60)
  Project Settings → Time → Maximum Allowed Timestep = 0.05

TODA lógica de jogo roda em FixedUpdate(). Nunca use deltaTime variável para física.
A interpolação para render suave é feita com Rigidbody2D.interpolation = Interpolate
OU manualmente armazenando prevPosition + lerpando no Update().

```csharp
// PhysicsWorld.cs
void FixedUpdate()
{
    spatialHash.Clear();
    foreach (var top in activeTops) spatialHash.Insert(top);

    foreach (var top in activeTops)
        top.OnTick(Time.fixedDeltaTime);

    foreach (var top in activeTops)
    {
        var neighbors = spatialHash.Query(top);
        foreach (var other in neighbors)
            if (other.Id > top.Id)
                ResolveCollision(top, other);
    }

    foreach (var top in activeTops)
        HandleArenaBoundary(top);

    particlePool.Update(Time.fixedDeltaTime);
    camera.UpdateShake(Time.fixedDeltaTime);
}
```

REGRA: Update() é APENAS para render e HUD. Zero lógica de jogo lá.


--------------------------------------------------------------------------------
  ABORDAGEM DE FÍSICA — HÍBRIDA
--------------------------------------------------------------------------------

NÃO use Rigidbody2D para o movimento principal dos tops.
Use Rigidbody2D apenas como carrier de posição com:
  rb.isKinematic = true
  rb.interpolation = RigidbodyInterpolation2D.Interpolate

A física de colisão, spin, e movimento é 100% custom (igual ao spec JS).
Isso dá controle total sobre spin decay, spinTransfer e habilidades.

Os colliders (CircleCollider2D) são usados APENAS para detecção via OverlapCircle,
NÃO para resolução automática de física. A resolução é feita em PhysicsWorld.cs.

Cada tick:
  1. Aplicar spin decay
  2. Aplicar atrito linear (slowdown)
  3. Mover: rb.MovePosition(new Vector2(top.X, top.Y))
  4. Resolver colisões (PhysicsWorld)
  5. Resolver boundary da arena


--------------------------------------------------------------------------------
  MODELO DE DADOS — TopBase.cs
--------------------------------------------------------------------------------

```csharp
public class TopBase : MonoBehaviour
{
    // Identificação
    public int Id;
    public string CharacterName;
    public Color CharacterColor;

    // Posição e movimento
    public float X, Y;
    public float VX, VY;
    public float Angle;       // orientação visual (radianos)
    public float Spin;        // velocidade angular = "HP"
    public float SpinMax;

    // Física
    public float Radius;
    public float Mass;
    public float Friction;        // decaimento de spin por tick
    public float Restitution;

    // Estado
    public bool IsAlive;
    public float Wobble;          // 0..1, intensidade do wobble
    public float WobbleAngle;
    public float BaseSpeed;       // velocidade linear base para cálculos

    // HP derivado
    public float HP => Spin;
    public float HPMax => SpinMax;
    public float HPRatio => Spin / SpinMax;

    // Hooks (override nos filhos)
    public virtual void OnTick(float dt) { }
    public virtual void OnCollide(TopBase other, float impactForce, float nx, float ny) { }
    public virtual void OnDeath() { }
    public virtual void OnRender() { }

    // Chamado por PhysicsWorld a cada tick
    public void Tick(float dt)
    {
        if (!IsAlive) return;

        // Spin decay
        Spin *= (1f - Friction);
        if (Spin < SpinMax * 0.02f) { Die(); return; }

        // Movimento
        X += VX * dt;
        Y += VY * dt;
        Angle += Spin * dt;

        // Atrito linear (frame-rate independent)
        float drag = Mathf.Pow(0.985f, 60f * dt);
        VX *= drag;
        VY *= drag;

        // Wobble
        Wobble = 1f - HPRatio;
        WobbleAngle += (5f + Wobble * 10f) * dt;

        // Sincronizar com Rigidbody2D para interpolação
        GetComponent<Rigidbody2D>().MovePosition(new Vector2(X, Y));

        OnTick(dt);
    }

    void Die()
    {
        IsAlive = false;
        OnDeath();
        EventBus.Emit("death", new DeathEvent { Top = this });
    }
}
```


--------------------------------------------------------------------------------
  FÍSICA — COLISÃO E ARENA
--------------------------------------------------------------------------------

```csharp
// PhysicsWorld.cs

const float SPIN_TRANSFER_RATIO = 0.015f;
const float BORDER_SPIN_DRAIN   = 0.008f;
const float ARENA_RADIUS        = 350f;

void ResolveCollision(TopBase a, TopBase b)
{
    float dx = b.X - a.X;
    float dy = b.Y - a.Y;
    float dist = Mathf.Sqrt(dx * dx + dy * dy);
    if (dist >= a.Radius + b.Radius || dist < 0.001f) return;

    float nx = dx / dist;
    float ny = dy / dist;

    // Separar
    float overlap = (a.Radius + b.Radius - dist) * 0.5f;
    a.X -= nx * overlap;  a.Y -= ny * overlap;
    b.X += nx * overlap;  b.Y += ny * overlap;

    // Velocidade relativa na normal
    float rvx = b.VX - a.VX;
    float rvy = b.VY - a.VY;
    float rvDotN = rvx * nx + rvy * ny;
    if (rvDotN > 0f) return;

    float e = Mathf.Min(a.Restitution, b.Restitution);
    float j = -(1f + e) * rvDotN / (1f / a.Mass + 1f / b.Mass);

    a.VX -= (j / a.Mass) * nx;  a.VY -= (j / a.Mass) * ny;
    b.VX += (j / b.Mass) * nx;  b.VY += (j / b.Mass) * ny;

    // Spin transfer
    float spinTransfer = Mathf.Abs(j) * SPIN_TRANSFER_RATIO;
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

    EventBus.Emit("collision", new CollisionEvent { A = a, B = b, Force = Mathf.Abs(j) });
    SpawnCollisionParticles(a, b, Mathf.Abs(j));
    ApplyCameraShake(Mathf.Abs(j));
}

void HandleArenaBoundary(TopBase top)
{
    float dx = top.X - (Screen.width * 0.5f);
    float dy = top.Y - (Screen.height * 0.5f);
    float dist = Mathf.Sqrt(dx * dx + dy * dy);
    float limit = ARENA_RADIUS - top.Radius;

    if (dist <= limit) return;

    float nx = dx / dist;
    float ny = dy / dist;
    top.X = (Screen.width * 0.5f)  + nx * limit;
    top.Y = (Screen.height * 0.5f) + ny * limit;

    float dot = top.VX * nx + top.VY * ny;
    top.VX -= 2f * dot * nx * top.Restitution;
    top.VY -= 2f * dot * ny * top.Restitution;
    top.Spin -= Mathf.Abs(dot) * BORDER_SPIN_DRAIN;
}
```


--------------------------------------------------------------------------------
  SPATIAL HASH — SpatialHash.cs
--------------------------------------------------------------------------------

```csharp
public class SpatialHash
{
    const int CELL_SIZE = 80;
    readonly Dictionary<long, List<TopBase>> cells = new();

    long Key(float x, float y)
    {
        int cx = Mathf.FloorToInt(x / CELL_SIZE);
        int cy = Mathf.FloorToInt(y / CELL_SIZE);
        return ((long)cx << 32) | (uint)cy;
    }

    public void Clear() => cells.Clear();

    public void Insert(TopBase top)
    {
        var k = Key(top.X, top.Y);
        if (!cells.TryGetValue(k, out var list))
            cells[k] = list = new List<TopBase>();
        list.Add(top);
    }

    public List<TopBase> Query(TopBase top)
    {
        int cx = Mathf.FloorToInt(top.X / CELL_SIZE);
        int cy = Mathf.FloorToInt(top.Y / CELL_SIZE);
        var result = new List<TopBase>();
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            long k = ((long)(cx + dx) << 32) | (uint)(cy + dy);
            if (cells.TryGetValue(k, out var list))
                result.AddRange(list);
        }
        return result;
    }
}
```


--------------------------------------------------------------------------------
  PERSONAGEM 1 — BERSERKER
--------------------------------------------------------------------------------

Stats base:
  SpinMax:     500
  Mass:        1.0
  Radius:      22
  Friction:    0.0015f
  Restitution: 0.8f
  Color:       #E8593C (laranja-vermelho)

Visual:
  Triângulo dentado (3 vértices com serrilhado). Quanto menos HP, mais
  "rachaduras" (linhas brancas saindo do centro). Cor satura em vermelho
  intenso com baixo HP.

Habilidade — FÚRIA:
  Para cada 10% de HP perdido, +15% de velocidade linear.
  +8% de spinTransfer causado em colisões por stack de fúria.
  Fúria Máxima (< 20% HP): tamanho visual +10% (apenas render).

```csharp
// BerserkerTop.cs
public class BerserkerTop : TopBase
{
    int furyStacks;

    void Awake()
    {
        CharacterName = "Berserker";
        SpinMax  = 500f;
        Spin     = SpinMax;
        Mass     = 1.0f;
        Radius   = 22f;
        Friction = 0.0015f;
        Restitution = 0.8f;
        CharacterColor = new Color(0.91f, 0.35f, 0.24f);
    }

    public override void OnTick(float dt)
    {
        furyStacks = Mathf.FloorToInt((1f - HPRatio) / 0.1f); // 0..10

        float speedMult = 1f + furyStacks * 0.15f;
        float currentSpeed = Mathf.Sqrt(VX * VX + VY * VY);
        float targetSpeed  = BaseSpeed * speedMult;

        if (currentSpeed > 0.1f && currentSpeed < targetSpeed)
        {
            float scale = targetSpeed / currentSpeed;
            VX *= scale;
            VY *= scale;
        }

        // Emitir brasas quando fúria > 5
        if (furyStacks > 5 && Random.value < 4f * dt)
            ParticlePool.Instance.EmitSpark(X, Y, CharacterColor, radius: 2f, speed: 40f);
    }

    public override void OnCollide(TopBase other, float impactForce, float nx, float ny)
    {
        // Bônus de spinTransfer baseado em fúria
        float bonusDamage = impactForce * PhysicsWorld.SPIN_TRANSFER_RATIO * (furyStacks * 0.08f);
        other.Spin -= bonusDamage;
        other.Spin = Mathf.Max(other.Spin, 0f);
    }

    public override void OnRender()
    {
        float visualScale = HPRatio < 0.2f ? 1.1f : 1.0f;
        float crackIntensity = 1f - HPRatio;
        // TopRenderer chama isso para aplicar scale e desenhar rachaduras
        TopRenderer.RenderBerserker(this, visualScale, crackIntensity, furyStacks);
    }

    public override void OnDeath()
    {
        ParticlePool.Instance.EmitDeath(X, Y, CharacterColor);
        CameraShake.Instance.Shake(18f);
    }
}
```

Renderização do Berserker (TopRenderer.cs):
  Corpo triangular com vértices ligeiramente irregulares (dentado).
  Rachaduras: Mathf.FloorToInt(crackIntensity * 5) linhas brancas saindo
  do centro, ângulos distribuídos, comprimento = Radius * 0.6f * crackIntensity.
  Partículas de brasa (sparks laranjas) emitidas continuamente quando furyStacks > 5.


--------------------------------------------------------------------------------
  PERSONAGEM 2 — PARASITA
--------------------------------------------------------------------------------

Stats base:
  SpinMax:     400
  Mass:        0.9
  Radius:      19
  Friction:    0.002f
  Restitution: 0.7f
  Color:       #2A9E70 (verde)

Visual:
  Corpo irregular/orgânico (polígono levemente assimétrico).
  Filamento pulsante (linha tracejada animada) conectando ao alvo infectado.
  Círculo pulsante no ponto de infecção do alvo.

Habilidade — INFECÇÃO:
  Ao colidir, aplica sanguessuga no alvo:
    - Drena 0.8% do SpinMax do alvo por segundo
    - Cura o Parasita com 50% do drain
    - Máximo de 3 sanguessugas no mesmo alvo
    - Dura 5s ou até alvo bater na borda (remove todas as sanguessugas)

```csharp
// ParasitaTop.cs
public struct Leech
{
    public TopBase Target;
    public float DrainRate;   // por segundo
    public float Elapsed;
    public float Duration;
}

public class ParasitaTop : TopBase
{
    readonly List<Leech> activeLeechesOnOthers = new(); // sanguessugas que EU apliquei

    void Awake()
    {
        CharacterName = "Parasita";
        SpinMax     = 400f;
        Spin        = SpinMax;
        Mass        = 0.9f;
        Radius      = 19f;
        Friction    = 0.002f;
        Restitution = 0.7f;
        CharacterColor = new Color(0.16f, 0.62f, 0.44f);
    }

    public override void OnCollide(TopBase other, float impactForce, float nx, float ny)
    {
        // Contar sanguessugas já aplicadas nesse alvo por mim
        int countOnTarget = 0;
        foreach (var l in activeLeechesOnOthers)
            if (l.Target == other) countOnTarget++;

        if (countOnTarget >= 3) return;

        activeLeechesOnOthers.Add(new Leech
        {
            Target    = other,
            DrainRate = other.SpinMax * 0.008f,
            Elapsed   = 0f,
            Duration  = 5f,
        });

        FloatingNumbers.Instance.Show(other.X, other.Y, "INFECTADO", CharacterColor, 14f);
    }

    public override void OnTick(float dt)
    {
        // Processar sanguessugas aplicadas por mim
        for (int i = activeLeechesOnOthers.Count - 1; i >= 0; i--)
        {
            var l = activeLeechesOnOthers[i];

            if (!l.Target.IsAlive) { activeLeechesOnOthers.RemoveAt(i); continue; }

            l.Elapsed += dt;
            if (l.Elapsed >= l.Duration) { activeLeechesOnOthers.RemoveAt(i); continue; }

            float drain = l.DrainRate * dt;
            l.Target.Spin -= drain;
            Spin = Mathf.Min(Spin + drain * 0.5f, SpinMax);

            activeLeechesOnOthers[i] = l;

            // Partícula de drenagem a cada 0.5s (via timer implícito em ParticlePool)
        }
    }

    // Chamado quando o alvo bate na borda (PhysicsWorld avisa via EventBus)
    void OnBorderHit(TopBase target)
    {
        activeLeechesOnOthers.RemoveAll(l => l.Target == target);
    }

    public override void OnRender()
    {
        TopRenderer.RenderParasita(this, activeLeechesOnOthers);
    }

    public override void OnDeath()
    {
        activeLeechesOnOthers.Clear();
        ParticlePool.Instance.EmitDeath(X, Y, CharacterColor);
        CameraShake.Instance.Shake(14f);
    }
}
```

Renderização do Parasita (TopRenderer.cs):
  Filamento: GL.Lines ou LineRenderer tracejado animado.
    - Para cada leech ativa: linha de (Parasita.X, Y) → (Target.X, Y)
    - Cor verde, alpha 0.7, dashOffset animado via Time.time
    - lineWidth: 1px
  Pulso no ponto de infecção:
    - Círculo de raio 4..8 pulsando com sin(Time.time * 6), alpha 0.6


--------------------------------------------------------------------------------
  SISTEMA DE PARTÍCULAS — ParticlePool.cs
--------------------------------------------------------------------------------

NUNCA instanciar partículas com Instantiate() dentro do loop.
Usar pool pré-alocado. Para Unity, a abordagem mais performática é
usar um único Mesh atualizado manualmente OU Unity's ParticleSystem em modo manual.

OPÇÃO RECOMENDADA: Unity ParticleSystem com emissão manual.

```csharp
// ParticlePool.cs — wrapper em torno de ParticleSystem manual
public class ParticlePool : MonoBehaviour
{
    public static ParticlePool Instance;
    ParticleSystem ps;

    void Awake()
    {
        Instance = this;
        ps = GetComponent<ParticleSystem>();
        // Configurar via código: loop=false, maxParticles=512
    }

    ParticleSystem.EmitParams emitParams = new();

    public void EmitSpark(float x, float y, Color color, float radius = 3f, float speed = 80f)
    {
        emitParams.position  = new Vector3(x, y, 0f);
        emitParams.velocity  = Random.insideUnitCircle * speed;
        emitParams.startSize = radius * 2f;
        emitParams.startColor = color;
        emitParams.startLifetime = 0.4f;
        ps.Emit(emitParams, 1);
    }

    public void EmitImpact(float x, float y, Color color, float force)
    {
        int sparks = force < 50f ? 4 : force < 200f ? 8 : 12;
        for (int i = 0; i < sparks; i++) EmitSpark(x, y, color, radius: 2.5f, speed: force * 0.5f);
        EmitRing(x, y, color);
        if (force >= 200f) EmitRing(x, y, color);
    }

    public void EmitRing(float x, float y, Color color)
    {
        // Ring expansivo: spawnar partículas em arco ou usar LineRenderer animado
        // Alternativa: pool de GameObjects com animação de escala (desaconselha-se)
        // Melhor: shader quad com ring mask (uma única draw call)
    }

    public void EmitDeath(float x, float y, Color color)
    {
        for (int i = 0; i < 20; i++) EmitSpark(x, y, color, radius: 4f, speed: 120f);
        EmitRing(x, y, color);
        EmitRing(x, y, color);
        EmitRing(x, y, color);
    }
}
```

BUDGET POR EVENTO:
  Impacto leve   (force < 50):   4 sparks + 1 ring
  Impacto médio  (force < 200):  8 sparks + 1 ring
  Impacto pesado (force >= 200): 12 sparks + 2 rings + 4 debris
  Morte:                         20 debris + 3 rings
  Habilidade:                    max 16 partículas


--------------------------------------------------------------------------------
  CAMERA SHAKE — CameraShake.cs
--------------------------------------------------------------------------------

```csharp
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    float shakeMag;
    const float DECAY = 8f;

    void Awake() => Instance = this;

    public void Shake(float magnitude)
        => shakeMag = Mathf.Min(shakeMag + magnitude, 20f);

    // Chamado por PhysicsWorld.FixedUpdate
    public void UpdateShake(float dt)
    {
        if (shakeMag < 0.1f) { shakeMag = 0f; return; }
        float ox = (Random.value - 0.5f) * 2f * shakeMag;
        float oy = (Random.value - 0.5f) * 2f * shakeMag;
        Camera.main.transform.localPosition = new Vector3(ox, oy, -10f);
        shakeMag -= DECAY * dt;
    }
}

// Calibração por força de impacto:
// force < 50   → Shake(1)
// force < 200  → Shake(4)
// force < 500  → Shake(8)
// force >= 500 → Shake(14)
// Morte        → Shake(18)
```


--------------------------------------------------------------------------------
  RENDERIZAÇÃO DOS TOPS — TopRenderer.cs
--------------------------------------------------------------------------------

Usar GL.Begin/End OU Graphics.DrawMesh com mesh gerada por código.
NÃO usar SpriteRenderer com sprites externos.

CAMADAS DE RENDER (por cima):
  1. Sombra no chão (elipse oval, alpha proporcional ao wobble)
  2. Corpo principal (forma geométrica rotacionada com Angle + tilt de wobble)
  3. HP ring (arco Canvas, LineRenderer circular)
  4. Efeitos de habilidade (filamentos, aura, etc.)

HP RING:
  LineRenderer circular em torno do top.
  Comprimento do arco = HPRatio * 360°.
  Cor: verde (HP alto) → amarelo → vermelho (HP baixo).
  hpColor(t): Mathf.Lerp entre Color.green, Color.yellow, Color.red baseado em t.

WOBBLE TILT:
  tiltAngle = Mathf.Sin(WobbleAngle) * Wobble * 0.3f  (radianos)
  Aplicar como rotação extra no eixo Z antes de Angle.

RENDER DE CADA PERSONAGEM:

  Berserker (triângulo dentado):
    Usar Mesh procedural com 3 vértices base + serrilhado nos lados.
    Atualizar vértices baseado em furyStacks (pequenos spikes crescem).

  Parasita (blob orgânico):
    Polígono de 8 vértices com offset irregular por vertex.
    Vértices levemente animados com Mathf.Sin(Time.time + i * 1.3f) * 0.05f.


--------------------------------------------------------------------------------
  HUD — HUDController.cs
--------------------------------------------------------------------------------

Usar Canvas separado (Screen Space - Overlay) com layer acima do game canvas.
Atualizar HUD no Update(), mas throttle para 30fps nos valores numéricos.

Componentes:
  [Esquerda — Top A]
    - Ícone/avatar (Image, cor do personagem)
    - Nome (TextMeshPro, bold 14px)
    - Barra de HP (Slider ou Image fill horizontal, 200px, cor dinâmica)
    - Texto HP: "48.2K / 100K" (TextMeshPro, monospace 12px)

  [Direita — Top B, espelhado]

  [Centro inferior — log de eventos]
    - TextMeshPro, some após 2s com DOTween Fade OU corrotina de alpha

```csharp
// HUDController.cs
float hudUpdateTimer;
const float HUD_THROTTLE = 1f / 30f;

void Update()
{
    hudUpdateTimer += Time.deltaTime;
    if (hudUpdateTimer < HUD_THROTTLE) return;
    hudUpdateTimer = 0f;

    UpdateHPBar(topA, hpBarA, hpTextA);
    UpdateHPBar(topB, hpBarB, hpTextB);
}

void UpdateHPBar(TopBase top, Slider bar, TMP_Text label)
{
    float ratio = top.HPRatio;
    bar.value = ratio;
    bar.fillRect.GetComponent<Image>().color = HPColor(ratio);
    label.text = $"{NumberFormat.Fmt(top.HP)} / {NumberFormat.Fmt(top.HPMax)}";
}
```


--------------------------------------------------------------------------------
  FLOATING NUMBERS — FloatingNumbers.cs
--------------------------------------------------------------------------------

Pool de TextMeshPro GameObjects pré-instanciados (max 12).
Ao receber dano: reativar um slot inativo, posicionar, animar subida + fade.

```csharp
// Calibração visual:
// dano < 1K    → 14px, branco
// dano >= 1K   → 18px, amarelo
// dano >= 100K → 24px, laranja
// Habilidade   → 28px, cor do personagem, outline branco
// Máx. 6 simultâneos por top
```


--------------------------------------------------------------------------------
  FORMATAÇÃO DE NÚMEROS — NumberFormat.cs
--------------------------------------------------------------------------------

```csharp
public static string Fmt(float n)
{
    if (n >= 1e12f) return $"{n / 1e12f:F1}T";
    if (n >= 1e9f)  return $"{n / 1e9f:F1}B";
    if (n >= 1e6f)  return $"{n / 1e6f:F1}M";
    if (n >= 1e3f)  return $"{n / 1e3f:F1}K";
    return Mathf.RoundToInt(n).ToString();
}
// Fmt(1234567f) → "1.2M"
// Fmt(430000f)  → "430.0K"
```


--------------------------------------------------------------------------------
  EVENT BUS — EventBus.cs
--------------------------------------------------------------------------------

```csharp
public static class EventBus
{
    static readonly Dictionary<string, Action<object>> listeners = new();

    public static void On(string evt, Action<object> cb)
    {
        if (!listeners.ContainsKey(evt)) listeners[evt] = null;
        listeners[evt] += cb;
    }

    public static void Off(string evt, Action<object> cb)
        => listeners[evt] -= cb;

    public static void Emit(string evt, object data)
    {
        if (listeners.TryGetValue(evt, out var cb)) cb?.Invoke(data);
    }
}

// Eventos usados:
// "collision"  → CollisionEvent { TopBase A, TopBase B, float Force }
// "death"      → DeathEvent     { TopBase Top }
// "spinChange" → SpinChangeEvent{ TopBase Top, float Delta }
// "borderHit"  → BorderHitEvent { TopBase Top }
```


--------------------------------------------------------------------------------
  ANIMAÇÃO DE MORTE
--------------------------------------------------------------------------------

Ao Spin cair abaixo de 2% do SpinMax:

  Fase 1 — WOBBLE MÁXIMO (0.3s):
    Lerp rápido de Wobble atual → 1.0
    IsPhysicsActive = false (parar colisões)

  Fase 2 — ESPIRAL DE QUEDA (0.8s):
    Spin → 0 linearmente
    tiltAngle → PI/2
    visualScale: 1 → 0

  Fase 3 — EXPLOSÃO:
    ParticlePool.EmitDeath()
    CameraShake.Shake(18)
    FloatingNumbers: "ELIMINADO" ou nome do vencedor

  Fase 4 — RESULTADO (1.5s depois):
    Fade-in overlay via CanvasGroup.alpha
    Nome do vencedor centralizado

Implementar como corrotina em GameManager.cs:
  StartCoroutine(DeathSequence(deadTop, winnerTop));


--------------------------------------------------------------------------------
  FLUXO DE PARTIDA — GameManager.cs
--------------------------------------------------------------------------------

```
1. Selecionar personagens (hardcoded: Berserker vs Parasita na v1)
2. Instanciar prefabs dos tops em posições opostas da arena
3. Dar velocidade inicial divergente (ex: VX = ±180f)
4. Countdown visual: 3, 2, 1... FIGHT! (corrotina no HUD)
5. Ativar PhysicsWorld.isRunning = true
6. Loop roda até OnDeath ser disparado
7. DeathSequence → tela de resultado
8. Botões: Rematch / Novo par
```


--------------------------------------------------------------------------------
  GAME CONFIG — GameConfig.cs (todas as constantes)
--------------------------------------------------------------------------------

```csharp
public static class GameConfig
{
    // Arena
    public const float ArenaRadius         = 350f;

    // Física
    public const float SpinTransferRatio   = 0.015f;
    public const float BorderSpinDrain     = 0.008f;
    public const float LinearDrag          = 0.985f;  // pow base por tick

    // Spatial Hash
    public const int   CellSize            = 80;

    // Partículas
    public const int   ParticlePoolSize    = 512;

    // Camera
    public const float ShakeDecay          = 8f;
    public const float ShakeMax            = 20f;

    // HUD
    public const float HudThrottleFps     = 30f;

    // Death
    public const float DeathSpinThreshold  = 0.02f;   // 2% do SpinMax
}
```


--------------------------------------------------------------------------------
  PERFORMANCE — REGRAS NUNCA VIOLAR
--------------------------------------------------------------------------------

  ✗ Nunca Instantiate/Destroy dentro de FixedUpdate ou Update
  ✗ Nunca usar GetComponent<>() no loop — cachear em Awake()
  ✗ Nunca usar Find(), FindObjectOfType() fora de inicialização
  ✗ Nunca usar LINQ (Where, Select, etc.) em arrays quentes
  ✗ Nunca layout queries de UI dentro do game loop
  ✓ for loop clássico em coleções quentes (não foreach com IEnumerable boxing)
  ✓ Pré-alocar List<> com capacidade explícita onde possível
  ✓ Usar structs para dados de partículas e leeches (evitar GC)
  ✓ Medir com Unity Profiler ao adicionar qualquer funcionalidade nova
  ✓ Target: FixedUpdate() < 2ms, Update()/render < 6ms para 2 agentes a 60fps


--------------------------------------------------------------------------------
  CLAUDE.md — COLOCAR NA RAIZ DO PROJETO UNITY
--------------------------------------------------------------------------------

```markdown
# Spinning Tops Royale

## Stack
Unity 2022.3 LTS, URP, C#, sem assets de terceiros.
Renderização procedural via GL/Mesh. Física 100% custom (não usar Rigidbody physics).

## Arquitetura
- Toda lógica de jogo em FixedUpdate() via PhysicsWorld.cs
- Update() apenas para render e HUD
- Pool de partículas manual (nunca Instantiate no loop)
- EventBus para desacoplar habilidades da física

## Personagens ativos
- BerserkerTop.cs — fúria escala com HP perdido
- ParasitaTop.cs  — sanguessuga drena spin do alvo

## Convenções
- Campos públicos no TopBase (não properties) para acesso O(1) na física
- Constantes centralizadas em GameConfig.cs
- Sem corrotinas para lógica de física; corrotinas apenas para sequências de UI

## Performance target
FixedUpdate < 2ms, render < 6ms para 2 agentes @ 60fps

## Próximos personagens planejados
Espelho, Coroa, Fenda, Sombra, Tremor, Poço (spec completo disponível)
```


================================================================================
  FIM DO SPEC UNITY
================================================================================
