# Spinning Tops Royale

## Stack
Unity 2022.3 LTS, URP, C#, sem assets de terceiros.
Renderização procedural via LineRenderer + Mesh. Física 100% custom (não usar Rigidbody physics).

## Arquitetura
- Toda lógica de jogo em `FixedUpdate()` via `PhysicsWorld.cs`
- `Update()` apenas para render (LineRenderers) e HUD
- Pool de partículas manual — nunca `Instantiate` no loop
- `EventBus` para desacoplar habilidades da física
- `SpatialHash` para broad-phase de colisão

## Estrutura de pastas (Assets/Scripts/)
```
Core/        GameConfig, EventBus, PhysicsWorld, ArenaController, GameManager
Tops/        TopBase, TopRenderer
Tops/Characters/  BerserkerTop, ParasitaTop
Systems/     SpatialHash, CameraShake, ParticlePool, FloatingNumbers
HUD/         HUDController
Utils/       Vec2Util, NumberFormat
```

## Personagens ativos
- `BerserkerTop.cs` — FÚRIA: +15% speed por 10% de HP perdido; +8% spinTransfer por stack
- `ParasitaTop.cs`  — INFECÇÃO: leech drena 0.8% SpinMax/s, cura 50%, máx 3 por alvo, 5s

## Convenções
- Campos públicos em TopBase (não properties) para acesso O(1) na física
- Constantes centralizadas em `GameConfig.cs`
- Sem corrotinas para lógica de física; corrotinas apenas para sequências de UI
- Sem `GetComponent<>()`, `Find()`, ou LINQ dentro de loops quentes
- for loops clássicos em coleções quentes (não foreach com boxing)

## Performance target
FixedUpdate < 2ms, render < 6ms para 2 agentes @ 60fps

## Próximos personagens planejados
Espelho, Coroa, Fenda, Sombra, Tremor, Poço (spec completo disponível no spec)
