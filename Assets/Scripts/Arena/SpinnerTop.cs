using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Arena/Spinner Top (Pião)")]
public class SpinnerTop : MonoBehaviour
{
    [SerializeField] ArenaBoundary arena;

    [SerializeField, Tooltip("Raio no plano XZ.")]
    float radius = 0.5f;

    [SerializeField]
    Vector3 initialImpulse = new Vector3(4f, 0f, 0f);

    [SerializeField]
    float initialSpinDegreesPerSecond = 720f;

    [SerializeField, Tooltip("Fração da velocidade perdida por segundo (0 = sem atrito).")]
    float linearVelocityDecayPerSecond = 0.05f;

    internal Vector3  _position;
    internal Vector3  _velocity;
    internal Collider _collider;
    internal float    Radius         => radius;
    internal float    DecayPerSecond => linearVelocityDecayPerSecond;

    float _spinDegPerSec;
    bool  _started;

    void Awake() => _collider = GetComponent<Collider>();

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

        _position      = transform.position;
        _velocity      = new Vector3(initialImpulse.x, 0f, initialImpulse.z);
        _spinDegPerSec = initialSpinDegreesPerSecond;
        _started       = true;
        arena.Register(this);
    }

    internal void ApplyToTransform(float dt)
    {
        transform.position = _position;
        transform.Rotate(0f, _spinDegPerSec * dt, 0f, Space.Self);
    }
}
