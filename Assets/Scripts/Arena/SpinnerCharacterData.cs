using UnityEngine;

/// <summary>
/// Stats base compartilhados por todos os personagens.
/// Estenda este SO para adicionar stats específicos de cada personagem.
/// </summary>
public class SpinnerCharacterData : ScriptableObject
{
    [Header("Identidade")]
    public string characterName  = "Unnamed";
    public Color  characterColor = Color.white;

    [Header("Física")]
    public float radius      = 88f;
    public float mass        = 1f;
    public float restitution = 0.8f;

    [Header("Movimento")]
    public float baseSpeed = 180f;

    [Header("Combate")]
    public float spinMax = 500f;
}
