using System.Collections.Generic;
using UnityEngine;

public class AlchemistSpinner : SpinnerBase
{
    [Header("Potion Throwing")]
    [SerializeField] private List<GameObject> potionPrefabs = new();
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private float throwInterval = 3f;

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
        if (potionPrefabs == null || potionPrefabs.Count == 0) return;

        GameObject prefab = potionPrefabs[_nextPotionIndex];
        _nextPotionIndex = (_nextPotionIndex + 1) % potionPrefabs.Count;

        if (prefab == null) return;

        Vector3 origin = throwOrigin != null ? throwOrigin.position : transform.position;
        GameObject flask = Instantiate(prefab, origin, Random.rotation);

        PotionProjectile proj = flask.GetComponent<PotionProjectile>();
        if (proj == null) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 randomDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        proj.Launch(origin, randomDir, GetComponent<Collider>());
    }
}
