using UnityEngine;

public class AlchemistSpinner : SpinnerBase
{
    [Header("Potion Throwing")]
    [SerializeField] private GameObject potionPrefab;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private float throwInterval = 3f;
    [SerializeField] private float potionSpeed = 6f;

    private float _throwTimer;
    private int _nextPotionIndex;

    protected override void Update()
    {
        base.Update();

        _throwTimer += Time.deltaTime;
        if (_throwTimer >= throwInterval)
        {
            _throwTimer = 0f;
            TryThrow();
        }
    }

    private void TryThrow()
    {
        Vector3 origin = throwOrigin != null ? throwOrigin.position : transform.position;

        GameObject flask = Instantiate(potionPrefab, origin, Random.rotation);
        PotionProjectile proj = flask.GetComponent<PotionProjectile>();
        if (proj == null) return;

        PotionType type = (PotionType)_nextPotionIndex;
        _nextPotionIndex = (_nextPotionIndex + 1) % 3;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 randomDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        Collider throwerCollider = GetComponent<Collider>();
        proj.Launch(origin, randomDir, potionSpeed, type, throwerCollider);
    }
}
