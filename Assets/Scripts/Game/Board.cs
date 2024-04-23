using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SquareSelectorCreator))]
public class Board : MonoBehaviour
{
    private const int BoardSize = 8;

    [SerializeField] private Transform bottomLeftSquareTransform;
    [SerializeField] private float squareSize;

    private Token[,] _grid;
    private Token _selectedToken;
    private GameController _controller;
    private UIManagerScript _uiManager;
    private SquareSelectorCreator _squareSelector;

    private void Awake()
    {
        _squareSelector = GetComponent<SquareSelectorCreator>();
        CreateGrid();
    }

    public void SetDependencies(GameController gameController, UIManagerScript uiManager)
    {
        _controller = gameController;
        _uiManager = uiManager;
    }

    private void CreateGrid()
    {
        _grid = new Token[BoardSize, BoardSize];
    }

    public Vector3 CalculatePositionFromCoords(Vector2Int coords)
    {
        return bottomLeftSquareTransform.position + new Vector3(coords.x * squareSize, 0, coords.y * squareSize);
    }

    public bool HasToken(Token token)
    {
        return _grid.Cast<Token>().Any(currentToken => token == currentToken);
    }

    public void OnSquareSelected(Vector3 inputPosition)
    {
        if (!_controller.IsGameInProgress()) return;
        
        Vector2Int coords = CalculateCoordsFromPosition(inputPosition);
        Token token = GetTokenOnSquare(coords);

        //a token is already selected
        if (_selectedToken)
        {
            if (_selectedToken.CanMoveTo(coords))
            {
                OnMoveSelectedToken(coords, _selectedToken);
            }
        }
        else //no currently selected token
        {
            //clicked on token from the same team
            if (token && _controller.IsTeamTurnActive(token.Team))
            {
                SelectToken(token);
            }
        }
    }

    private void OnMoveSelectedToken(Vector2Int coords, Token token)
    {
        if (HasSquareOpponentToken(coords))
        {
            var isOpponentDead = AttackToken(coords);
            if (isOpponentDead)
            {
                TakeToken(coords);
                
                UpdateBoardOnTokenMove(coords, token.OccupiedSquare, token, null);
                token.MoveToken(coords);
            }
        }
        else
        {
            UpdateBoardOnTokenMove(coords, token.OccupiedSquare, token, null);
            token.MoveToken(coords);
        }
        
        DeselectToken();
        _controller.UseActivePlayerActionPoint();
    }

    private bool HasSquareOpponentToken(Vector2Int coords)
    {
        var token = GetTokenOnSquare(coords);
        return token && !token.IsFromSameTeam(_selectedToken);
    }

    private bool AttackToken(Vector2Int coords)
    {
        var token = GetTokenOnSquare(coords);
        var damage = Math.Max(0, _selectedToken.Attack - token.Defence);
        token.Health -= damage;

        return token.Health <= 0;
    }

    private void TakeToken(Vector2Int coords)
    {
        var token = GetTokenOnSquare(coords);
        _grid[token.OccupiedSquare.x, token.OccupiedSquare.y] = null;
        _controller.OnTokenRemoved(token);
    }

    private void EndTurn()
    {
        _controller.EndTurn();
    }

    private void UpdateBoardOnTokenMove(Vector2Int newCoords, Vector2Int oldCoords, Token newToken, Token oldToken)
    {
        _grid[oldCoords.x, oldCoords.y] = oldToken;
        _grid[newCoords.x, newCoords.y] = newToken;
    }

    private void SelectToken(Token token)
    {
        _selectedToken = token;
        _selectedToken.SelectAvailableSquares();
        var moves = _selectedToken.availableMoves;
        ShowAvailableMovesSquares(moves);
    }

    private void ShowAvailableMovesSquares(List<Vector2Int> moves)
    {
        Dictionary<Vector3,bool> squareData = new();

        foreach (var move in moves)
        {
            var position = CalculatePositionFromCoords(move);
            var isFree = !GetTokenOnSquare(move);
            squareData.Add(position, isFree);
        }

        _squareSelector.ShowSelection(squareData);
    }

    private void DeselectToken()
    {
        _selectedToken = null;
        _squareSelector.ClearSelection();
    }

    public Token GetTokenOnSquare(Vector2Int coords)
    {
        return CheckValidCoords(coords) ? _grid[coords.x, coords.y] : null;
    }

    public bool CheckValidCoords(Vector2Int coords)
    {
        return coords.x is >= 0 and < BoardSize
               && coords.y is >= 0 and < BoardSize;
    }

    private Vector2Int CalculateCoordsFromPosition(Vector3 inputPosition)
    {
        var localPosition = transform.InverseTransformPoint(inputPosition);
        int x = Mathf.FloorToInt(localPosition.x / squareSize) + (BoardSize / 2);
        int y = Mathf.FloorToInt(localPosition.z / squareSize) + (BoardSize / 2);

        return new Vector2Int(x, y);
    }

    public void SetTokenOnBoard(Vector2Int coords, Token token)
    {
        if (CheckValidCoords(coords))
        {
            _grid[coords.x, coords.y] = token;
        }
    }
}