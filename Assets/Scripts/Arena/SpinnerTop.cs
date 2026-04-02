using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Arena/Spinner Top (Pião)")]
public class SpinnerTop : MonoBehaviour
{
    [SerializeField] ArenaBoundary        arena;
    [SerializeField] SpinnerCharacterData characterData;

    [SerializeField, Tooltip("Raio no plano XZ.")]
    float radius = 0.5f;

    [SerializeField, Tooltip("Impulso inicial de direção/velocidade (usado sem agente de IA).")]
    Vector3 initialImpulse = new Vector3(4f, 0f, 0f);

    // Estado de simulação — acessado pela ArenaBoundary e SpinnerAgent
    internal Vector3          _position;
    internal Vector3          _velocity;
    internal Collider         _collider;
    internal SpinnerAgent     Agent         { get; private set; }
    internal SpinnerStats     Stats         { get; private set; }
    internal float            Radius        => radius;
    internal SpinnerCharacterData CharacterData => characterData;

    bool _started;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        Agent     = GetComponent<SpinnerAgent>();
    }

    void OnEnable()  { if (_started) arena?.Register(this); }
    void OnDisable() { arena?.Unregister(this); }

    void Start()
    {
        if (arena == null)
        {
            Debug.LogError($"{name}: atribua a Arena Boundary.", this);
            enabled = false;
            return;
        }
        if (characterData == null)
        {
            Debug.LogError($"{name}: atribua um Character Data.", this);
            enabled = false;
            return;
        }

        Stats     = new SpinnerStats(characterData);
        _position = transform.position;
        _velocity = new Vector3(initialImpulse.x, 0f, initialImpulse.z);
        _started  = true;

        Stats.OnDamaged  += lost => Debug.Log($"[{name}] -{lost:F0}°/s  →  {Stats.CurrentSpinSpeed:F0}°/s");
        Stats.OnDefeated += OnDefeated;

        arena.Register(this);
    }

    void OnDefeated()
    {
        _velocity = Vector3.zero;
        Debug.Log($"[Battle] {name} ({characterData.characterName}) foi derrotado!");
    }

    internal void ApplyToTransform(float dt)
    {
        transform.position = _position;
        transform.Rotate(0f, Stats.CurrentSpinSpeed * dt, 0f, Space.Self);
    }
}
