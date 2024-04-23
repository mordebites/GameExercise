using System.Collections.Generic;
using System.Linq;
using Enums;

public class Player
{
    public TeamColour team { get; set; }
    public Board board { get; set; }
    public List<Token> activeTokens { get; private set; }


    public Player(TeamColour team, Board board)
    {
        this.team = team;
        this.board = board;
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

    public void GenerateAllPossibleMoves()
    {
        foreach (var token in activeTokens.Where(token => board.HasToken(token)))
        {
            token.SelectAvailableSquares();
        }
    }

    public Token[] GetAttackingTokens<T>() where T : Token
    {
        return activeTokens.Where(p => p.IsAttackingTokenOfType<T>()).ToArray();
    }

    public Token[] GetTokensOfType<T>() where T: Token
    {
        return activeTokens.Where(token => token is T).ToArray();
    }
}