using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Arena/Arena Boundary")]
public class ArenaBoundary : MonoBehaviour
{
    [SerializeField, Tooltip("Largura (X) e profundidade (Z) da área interna.")]
    Vector2 innerSize = new Vector2(20f, 20f);

    [SerializeField] float wallHeight    = 3f;
    [SerializeField] float wallThickness = 0.5f;
    [SerializeField] bool  showVisuals   = true;

    readonly List<SpinnerTop> _spinners = new List<SpinnerTop>();

    bool _agentsPaired;

    const string WallNorth = "Wall_North";
    const string WallSouth = "Wall_South";
    const string WallEast  = "Wall_East";
    const string WallWest  = "Wall_West";

    public Vector2 InnerHalfExtents => innerSize * 0.5f;

    internal void Register(SpinnerTop s)
    {
        if (s == null || _spinners.Contains(s)) return;
        _spinners.Add(s);
        _agentsPaired = false; // re-pareia quando um novo pião entra
    }

    internal void Unregister(SpinnerTop s) { _spinners.Remove(s); }

    // ── Simulação ────────────────────────────────────────────────────────────

    void Update()
    {
        if (!Application.isPlaying || _spinners.Count == 0) return;

        for (int i = _spinners.Count - 1; i >= 0; i--)
            if (_spinners[i] == null) _spinners.RemoveAt(i);

        if (!_agentsPaired) PairAgents();

        float dt = Time.deltaTime;

        // CCD para bordas
        float remaining = dt;
        for (int iter = 0; iter < 64 && remaining > 1e-6f; iter++)
        {
            float tNext  = remaining;
            int   eIdx   = -1, eAxis = -1;
            bool  eIsMin = false;

            for (int i = 0; i < _spinners.Count; i++)
            {
                float t = WallTime(_spinners[i], tNext, out int axis, out bool isMin);
                if (t < tNext - 1e-8f)
                { tNext = t; eIdx = i; eAxis = axis; eIsMin = isMin; }
            }

            foreach (var s in _spinners)
            {
                s._position.x += s._velocity.x * tNext;
                s._position.z += s._velocity.z * tNext;
            }
            remaining -= tNext;

            if (eIdx < 0) break;
            ResolveWall(_spinners[eIdx], eAxis, eIsMin);
        }

        // Colisão entre piões via colliders (dano + impulso)
        ResolveSpinnerCollisions();

        // Agentes atualizam direção após a física
        for (int i = 0; i < _spinners.Count; i++)
            _spinners[i].Agent?.Tick(dt);

        foreach (var s in _spinners)
            s.ApplyToTransform(dt);
    }

    // Conecta cada agente ao seu oponente (primeiro pião diferente da lista).
    void PairAgents()
    {
        for (int i = 0; i < _spinners.Count; i++)
        {
            var agent = _spinners[i].Agent;
            if (agent == null) continue;
            for (int j = 0; j < _spinners.Count; j++)
            {
                if (i != j) { agent.SetOpponent(_spinners[j]); break; }
            }
        }
        _agentsPaired = true;
    }

    // Detecta colisão com Physics.ComputePenetration.
    // Iter 0: aplica dano + impulso elástico.
    // Iters seguintes: só corrige posição (vRel já é ≤ 0 após o impulso).
    void ResolveSpinnerCollisions()
    {
        if (_spinners.Count < 2) return;

        const int iterations = 3;

        for (int iter = 0; iter < iterations; iter++)
        for (int i = 0; i < _spinners.Count; i++)
        for (int j = i + 1; j < _spinners.Count; j++)
        {
            var a = _spinners[i];
            var b = _spinners[j];

            if (a._collider == null || b._collider == null) continue;

            if (!Physics.ComputePenetration(
                a._collider, a._position, a.transform.rotation,
                b._collider, b._position, b.transform.rotation,
                out Vector3 dir, out float dist)) continue;

            // dir aponta de B→A; zeramos Y para manter no plano XZ
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-8f) continue;
            dir.Normalize();

            float ma     = a.Radius * a.Radius;
            float mb     = b.Radius * b.Radius;
            float totalM = ma + mb;

            // Separa posições proporcionalmente à massa
            a._position.x += dir.x * (dist * mb / totalM);
            a._position.z += dir.z * (dist * mb / totalM);
            b._position.x -= dir.x * (dist * ma / totalM);
            b._position.z -= dir.z * (dist * ma / totalM);

            // Normal de A→B = -dir
            float nx   = -dir.x, nz = -dir.z;
            float vRel = (a._velocity.x - b._velocity.x) * nx
                       + (a._velocity.z - b._velocity.z) * nz;

            if (vRel <= 0f) continue; // já se afastando — só posição foi corrigida

            // Dano proporcional à velocidade de impacto
            a.Stats.TakeDamage(vRel, b.Stats);
            b.Stats.TakeDamage(vRel, a.Stats);

            // Impulso elástico (massa ∝ raio²)
            float impulse  = 2f * vRel / totalM;
            a._velocity.x -= mb * impulse * nx;
            a._velocity.z -= mb * impulse * nz;
            b._velocity.x += ma * impulse * nx;
            b._velocity.z += ma * impulse * nz;
        }
    }

    // ── Detecção de borda ─────────────────────────────────────────────────────

    // Retorna o tempo até o spinner atingir uma borda (dentro de maxT).
    float WallTime(SpinnerTop s, float maxT, out int hitAxis, out bool hitMinSide)
    {
        Transform at   = transform;
        Vector2   half = InnerHalfExtents;
        float r = Mathf.Max(0.001f, s.Radius);

        float minX = -half.x + r, maxX = half.x - r;
        float minZ = -half.y + r, maxZ = half.y - r;

        Vector3 lp = at.InverseTransformPoint(s._position);
        Vector3 lv = at.InverseTransformDirection(s._velocity);

        hitAxis    = -1;
        hitMinSide = false;
        float tMin = maxT;

        CheckAxis(lv.x, lp.x, minX, maxX, 0, ref tMin, ref hitAxis, ref hitMinSide);
        CheckAxis(lv.z, lp.z, minZ, maxZ, 2, ref tMin, ref hitAxis, ref hitMinSide);

        return tMin;
    }

    static void CheckAxis(float vel, float pos, float min, float max,
                          int axis, ref float tMin, ref int hitAxis, ref bool hitMinSide)
    {
        if (vel < -1e-8f)
        {
            float t = (pos - min) / -vel;
            if (t >= 0f && t < tMin) { tMin = t; hitAxis = axis; hitMinSide = true; }
        }
        else if (vel > 1e-8f)
        {
            float t = (max - pos) / vel;
            if (t >= 0f && t < tMin) { tMin = t; hitAxis = axis; hitMinSide = false; }
        }
    }

    void ResolveWall(SpinnerTop s, int hitAxis, bool hitMinSide)
    {
        if (hitAxis < 0) return;

        Transform at = transform;
        Vector3 lv = at.InverseTransformDirection(s._velocity);

        if (hitAxis == 0) lv.x = -lv.x;
        else              lv.z = -lv.z;

        // Garante que a posição fique exatamente na borda
        Vector2 half = InnerHalfExtents;
        float   r    = Mathf.Max(0.001f, s.Radius);
        Vector3 lp   = at.InverseTransformPoint(s._position);
        lp.x = Mathf.Clamp(lp.x, -half.x + r, half.x - r);
        lp.z = Mathf.Clamp(lp.z, -half.y + r, half.y - r);

        Vector3 wp = at.TransformPoint(lp);
        wp.y = s._position.y;
        s._position = wp;

        Vector3 wv = at.TransformDirection(lv);
        s._velocity.x = wv.x;
        s._velocity.z = wv.z;
        s._velocity.y = 0f;
    }

    // ── Paredes visuais ───────────────────────────────────────────────────────

    void OnEnable()   => Rebuild();
    void OnValidate() => Rebuild();

    void Rebuild()
    {
        float w = Mathf.Max(0.01f, innerSize.x);
        float d = Mathf.Max(0.01f, innerSize.y);
        float h = Mathf.Max(0.01f, wallHeight);
        float t = Mathf.Max(0.01f, wallThickness);

        float halfW   = w * 0.5f;
        float halfD   = d * 0.5f;
        float yCenter = h * 0.5f;

        EnsureWall(WallNorth, new Vector3(0f,               yCenter,  halfD + t * 0.5f), new Vector3(w + 2f * t, h, t));
        EnsureWall(WallSouth, new Vector3(0f,               yCenter, -halfD - t * 0.5f), new Vector3(w + 2f * t, h, t));
        EnsureWall(WallEast,  new Vector3( halfW + t * 0.5f, yCenter, 0f),               new Vector3(t, h, d));
        EnsureWall(WallWest,  new Vector3(-halfW - t * 0.5f, yCenter, 0f),               new Vector3(t, h, d));
    }

    void EnsureWall(string childName, Vector3 localPos, Vector3 size)
    {
        Transform child = transform.Find(childName);
        GameObject go;

        if (child == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = childName;
            go.transform.SetParent(transform, false);

            var col = go.GetComponent<Collider>();
            if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
        }
        else { go = child.gameObject; }

        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = size;

        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.enabled = showVisuals;
    }

    void OnDrawGizmos()
    {
        float w = Mathf.Max(0.01f, innerSize.x);
        float d = Mathf.Max(0.01f, innerSize.y);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color  = new Color(0.2f, 0.85f, 0.35f, 0.6f);
        Gizmos.DrawWireCube(new Vector3(0f, 0.01f, 0f), new Vector3(w, 0.02f, d));
    }
}
