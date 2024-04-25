using System;
using System.Linq;
using Enums;
using UnityEngine;

[Serializable]
public class TokenStats
{
    public TokenType tokenType;
    public int health;
    public int attack;
    public int defence;
}

[CreateAssetMenu(menuName = "Scriptable Objects/TokenSet/Stats")]
public class TokenSetStats : ScriptableObject
{
    [SerializeField] private TokenStats[] tokenStats;

    public TokenStats GetStatsForType(TokenType type)
    {
        try
        {
            return tokenStats.First(stat => stat.tokenType == type);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return null;
        }
    }
}
