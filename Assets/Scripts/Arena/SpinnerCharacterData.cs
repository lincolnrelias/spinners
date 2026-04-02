using UnityEngine;

/// <summary>
/// Dados estáticos de um personagem de pião.
/// Crie um asset por personagem: Assets → Create → Spinners → Character Data.
/// No futuro, adicione skills, visuais e modificadores aqui.
/// </summary>
[CreateAssetMenu(fileName = "New Spinner Character", menuName = "Spinners/Character Data")]
public class SpinnerCharacterData : ScriptableObject
{
    [Header("Identidade")]
    public string characterName = "Default";

    [Header("Movimento")]
    [Tooltip("Velocidade de movimento no plano XZ (m/s).")]
    public float movementSpeed = 5f;

    [Header("Giro")]
    [Tooltip("Velocidade de giro inicial (graus/s).")]
    public float maxSpinSpeed = 720f;
    [Tooltip("Velocidade de giro mínima — abaixo disso o pião é derrotado.")]
    public float minSpinSpeed = 60f;

    [Header("Combate")]
    [Tooltip("Graus/s de giro perdidos por 1 m/s de velocidade de impacto recebida.")]
    public float spinLossPerImpact = 40f;
    [Tooltip("Multiplicador do dano causado a outros piões.")]
    public float damageDealtMultiplier = 1f;
    [Tooltip("Multiplicador do dano sofrido.")]
    public float damageReceivedMultiplier = 1f;
}
