using System.Collections.Generic;
using System.Linq;
using Enums;

public class Player
{
    public TeamColour team { get; set; }
    public List<Token> activeTokens { get; private set; }


    public Player(TeamColour team)
    {
        this.team = team;
        activeTokens = new List<Token>();
    }

    public void AddToken(Token token)
    {
        if (!activeTokens.Contains(token))
            activeTokens.Add(token);
    }

    public void RemoveToken(Token token)
    {
        if (activeTokens.Contains(token))
            activeTokens.Remove(token);
    }

    public void ClearDefendingTokens()
    {
        foreach (var token in activeTokens)
        {
            token.IsDefending = false;
        }   
    }
}