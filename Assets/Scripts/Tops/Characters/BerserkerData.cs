using UnityEngine;

/// <summary>
/// Stats do Berserker. Crie via Assets → Create → Spinners → Berserker Data.
/// </summary>
[CreateAssetMenu(fileName = "BerserkerData", menuName = "Spinners/Berserker Data")]
public class BerserkerData : SpinnerCharacterData
{
    [Header("Fúria")]
    [Tooltip("Bônus de velocidade por stack de fúria (cada 10% de HP perdido = 1 stack).")]
    public float furySpeedBonusPerStack       = 0.15f;

    [Tooltip("Bônus de spinTransfer por stack de fúria aplicado ao dano de colisão.")]
    public float furySpinTransferBonusPerStack = 0.08f;
}
