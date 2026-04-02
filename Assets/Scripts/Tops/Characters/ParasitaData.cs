using UnityEngine;

/// <summary>
/// Stats do Parasita. Crie via Assets → Create → Spinners → Parasita Data.
/// </summary>
[CreateAssetMenu(fileName = "ParasitaData", menuName = "Spinners/Parasita Data")]
public class ParasitaData : SpinnerCharacterData
{
    [Header("Infecção")]
    [Tooltip("Fração do SpinMax do alvo drenada por segundo por leech.")]
    public float leechDrainRate      = 0.008f;

    [Tooltip("Duração de cada leech em segundos.")]
    public float leechDuration       = 5f;

    [Tooltip("Máximo de leeches simultâneos no mesmo alvo.")]
    public int   maxLeechesPerTarget = 3;

    [Tooltip("Fração do dano drenado que vira cura para o Parasita.")]
    public float leechHealRatio      = 0.5f;
}
