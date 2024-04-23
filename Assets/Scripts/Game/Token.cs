using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enums;
using UnityEngine;

[RequireComponent(typeof(MaterialSetter))]
public abstract class Token : MonoBehaviour
{
    private const int MaxTilesPerActionPoint = 1;
    
    private readonly Vector2Int[] _directions =
        { Vector2Int.left, Vector2Int.up, Vector2Int.right, Vector2Int.down };
    
    private MaterialSetter _materialSetter;
    private Board Board { get; set; }

    public int Health { get; set; }
    public int Attack { get; protected set; }
    public int Defence { get; protected set; }

    public bool IsDefending { get; set; }
    public Vector2Int OccupiedSquare { get; private set; }
    public TeamColour Team { get; private set; }
    public List<Vector2Int> availableMoves;
    
    private void Awake()
    {
        availableMoves = new List<Vector2Int>();
        _materialSetter = GetComponent<MaterialSetter>();
    }

    public void SelectAvailableSquares()
    {
        availableMoves.Clear();

        const float range = MaxTilesPerActionPoint;
        foreach (var direction in _directions)
        {
            for (var i = 1; i <= range; i++)
            {
                var nextCoords = OccupiedSquare + direction * i;
                var token = Board.GetTokenOnSquare(nextCoords);
                
                if (!Board.CheckValidCoords(nextCoords))
                    break;
                if (!token)
                    TryAddMove(nextCoords);
                else if (!token.IsFromSameTeam(this))
                {
                    if (token.IsDefending) continue;
                    
                    TryAddMove(nextCoords);
                    break;
                }
                else if (token.IsFromSameTeam(this))
                    break;
            }
        }
    }

    public void SetMaterial(Material material)
    {
        _materialSetter.SetSingleMaterial(material);
    }

    public bool IsFromSameTeam(Token token)
    {
        return Team == token.Team;
    }

    public bool CanMoveTo(Vector2Int coords)
    {
        return !IsDefending && availableMoves.Contains(coords);
    }

    private void TryAddMove(Vector2Int coords)
    {
        availableMoves.Add(coords);
    }

    public void SetData(Vector2Int coords, TeamColour colour, Board board)
    {
        OccupiedSquare = coords;
        Team = colour;
        Board = board;
        transform.position = board.CalculatePositionFromCoords(coords);
    }

    public virtual void MoveToken(Vector2Int coords)
    {
        Vector3 targetPosition = Board.CalculatePositionFromCoords(coords);
        OccupiedSquare = coords;
        transform.position = targetPosition;
    }

    public bool IsAttackingTokenOfType<T>() where T : Token
    {
        return availableMoves.Any(square => Board.GetTokenOnSquare(square) is T);
    }
}
