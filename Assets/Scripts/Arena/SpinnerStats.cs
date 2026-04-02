using System;
using UnityEngine;

/// <summary>
/// Estado de combate em tempo de execução de um pião.
/// Separa dados mutáveis (giro atual) dos dados estáticos do personagem.
/// </summary>
public class SpinnerStats
{
    public SpinnerCharacterData Data            { get; }
    public float                CurrentSpinSpeed { get; private set; }
    public bool                 IsDefeated       => CurrentSpinSpeed <= Data.minSpinSpeed;

    /// <summary>Disparado com os graus/s de giro perdidos neste hit.</summary>
    public event Action<float> OnDamaged;
    public event Action        OnDefeated;

    bool _defeatFired;

    public SpinnerStats(SpinnerCharacterData data)
    {
        Data             = data;
        CurrentSpinSpeed = data.maxSpinSpeed;
    }

    /// <summary>
    /// Aplica dano de impacto.
    /// impactSpeed = velocidade relativa de aproximação no momento da colisão (m/s).
    /// </summary>
    public void TakeDamage(float impactSpeed, SpinnerStats attacker)
    {
        if (IsDefeated) return;

        float damage = impactSpeed
                     * attacker.Data.damageDealtMultiplier
                     * Data.damageReceivedMultiplier
                     * Data.spinLossPerImpact;

        float before = CurrentSpinSpeed;
        CurrentSpinSpeed = Mathf.Max(Data.minSpinSpeed, CurrentSpinSpeed - damage);
        float lost = before - CurrentSpinSpeed;

        if (lost > 0f)
            OnDamaged?.Invoke(lost);

        if (IsDefeated && !_defeatFired)
        {
            _defeatFired = true;
            OnDefeated?.Invoke();
        }
    }
}
