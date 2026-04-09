using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] private GameObject matchFinishedContainer;
    [SerializeField] private TextMeshProUGUI matchFinishedText;

    private readonly Dictionary<SpinnerBase, float> spinnerHealth = new();
    private readonly List<SpinnerBase> activeSpinners = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        foreach (SpinnerBase spinner in FindObjectsByType<SpinnerBase>(FindObjectsSortMode.None))
        {
            activeSpinners.Add(spinner);
            spinnerHealth[spinner] = spinner.StartingHealth;
        }
    }

    public void RegisterDamage(SpinnerBase spinner, float amount)
    {
        if (!spinnerHealth.ContainsKey(spinner)) return;

        spinnerHealth[spinner] -= amount;
        if (spinnerHealth[spinner] <= 0f)
        {
            spinnerHealth.Remove(spinner);
            activeSpinners.Remove(spinner);
            spinner.Die();
            CheckMatchEnd();
        }
    }

    private void CheckMatchEnd()
    {
        if (activeSpinners.Count == 1)
            MatchFinished(activeSpinners[0]);
    }

    private void MatchFinished(SpinnerBase winner)
    {
        Time.timeScale = 0.1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (matchFinishedContainer != null)
            matchFinishedContainer.SetActive(true);

        if (matchFinishedText != null)
            matchFinishedText.text = $"{winner.gameObject.name} wins!";

        Debug.Log($"Match finished! Winner: {winner.gameObject.name}");
    }
}