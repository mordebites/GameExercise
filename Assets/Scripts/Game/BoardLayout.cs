using System;
using Enums;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Board/Layout")]
public class BoardLayout : ScriptableObject
{
    [Serializable]
    private class BoardSquareSetup
    {
        public Vector2Int position;
        public TokenType type;
        public TeamColour colour;
    }

    [SerializeField] private BoardSquareSetup[] boardSquares;

    public int GetTokenCount()
    {
        return boardSquares.Length;
    }

    public Vector2Int GetSquareCoordsAtIndex(int index)
    {
        if (boardSquares.Length > index)
        {
            return new Vector2Int(boardSquares[index].position.x - 1, boardSquares[index].position.y - 1);
        }

        Debug.LogError("Token index out of range");
        return new Vector2Int(-1, -1);
    }

    public string GetTokenNameAtIndex(int index)
    {
        if (boardSquares.Length > index)
        {
            return boardSquares[index].type.ToString();
        }

        Debug.LogError("Token index out of range");
        return null;
    }

    public TeamColour GetSquareTeamColourAtIndex(int index)
    {
        if (boardSquares.Length > index)
        {
            return boardSquares[index].colour;
        }

        Debug.LogError("Token index out of range");
        return TeamColour.Black;
    }
}
