using UnityEngine;

/// <summary>
/// Agente de IA que controla a direção de movimento de um SpinnerTop.
/// Herde esta classe e sobrescreva <see cref="UpdateDirection"/> para criar
/// estratégias diferentes (agressivo, defensivo, aleatório, etc.).
/// </summary>
[RequireComponent(typeof(SpinnerTop))]
[DisallowMultipleComponent]
[AddComponentMenu("Arena/Spinner Agent (IA)")]
public class SpinnerAgent : MonoBehaviour
{
    [SerializeField, Tooltip("Intervalo entre reavaliações de direção (segundos).")]
    float reactionTime = 0.25f;

    [SerializeField, Tooltip("Oponente alvo. Se vazio, a Arena preenche automaticamente.")]
    SpinnerTop manualOpponent;

    protected SpinnerTop Spinner  { get; private set; }
    protected SpinnerTop Opponent { get; private set; }

    float _timer;

    void Awake()
    {
        Spinner  = GetComponent<SpinnerTop>();
        Opponent = manualOpponent;
    }

    /// <summary>Chamado pela ArenaBoundary se nenhum oponente manual foi atribuído.</summary>
    internal void SetOpponent(SpinnerTop opponent)
    {
        if (manualOpponent != null) return;
        Opponent = opponent;
    }

    /// <summary>Chamado pela ArenaBoundary a cada frame após a simulação física.</summary>
    internal void Tick(float dt)
    {
        if (Spinner?.Stats == null || Spinner.Stats.IsDefeated) return;

        _timer -= dt;
        if (_timer > 0f) return;
        _timer = reactionTime;

        UpdateDirection();
    }

    /// <summary>
    /// Define a velocidade do pião. Sobrescreva para estratégias customizadas.
    /// Comportamento padrão: mover diretamente em direção ao oponente.
    /// </summary>
    protected virtual void UpdateDirection()
    {
        if (Opponent == null) return;

        Vector3 toOpponent = Opponent._position - Spinner._position;
        toOpponent.y = 0f;
        if (toOpponent.sqrMagnitude < 0.001f) return;

        Spinner._velocity = toOpponent.normalized * Spinner.CharacterData.movementSpeed;
    }
}
