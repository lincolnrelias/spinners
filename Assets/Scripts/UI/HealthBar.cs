using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private SpinnerBase spinner;
    [SerializeField] private Image fillImage;
    [SerializeField] private float smoothSpeed = 5f;

    private float targetFill = 1f;

    private void OnEnable()
    {
        GameStateManager.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        GameStateManager.OnHealthChanged -= OnHealthChanged;
    }

    private void Start()
    {
        fillImage.fillAmount = 1f;
    }

    private void Update()
    {
        if (fillImage.fillAmount != targetFill)
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.unscaledDeltaTime * smoothSpeed);
    }

    private void OnHealthChanged(SpinnerBase changed, float current, float max)
    {
        if (changed != spinner) return;
        targetFill = current / max;
    }
}
