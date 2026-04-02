using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    float _shakeMag;
    Vector3 _basePos;

    void Awake()
    {
        Instance = this;
        _basePos = new Vector3(0f, 0f, -10f);
    }

    public void Shake(float magnitude)
        => _shakeMag = Mathf.Min(_shakeMag + magnitude, GameConfig.ShakeMax);

    // Called by PhysicsWorld every FixedUpdate
    public void UpdateShake(float dt)
    {
        if (_shakeMag < 0.1f)
        {
            _shakeMag = 0f;
            Camera.main.transform.localPosition = _basePos;
            return;
        }
        float ox = (Random.value - 0.5f) * 2f * _shakeMag;
        float oy = (Random.value - 0.5f) * 2f * _shakeMag;
        Camera.main.transform.localPosition = _basePos + new Vector3(ox, oy, 0f);
        _shakeMag -= GameConfig.ShakeDecay * dt;
        if (_shakeMag < 0f) _shakeMag = 0f;
    }
}
