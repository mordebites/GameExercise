using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SquareSelectorCreator))]
public class Board : MonoBehaviour
{
    public const int BoardSize = 8;

    [SerializeField] private Transform bottomLeftSquareTransform;
    [SerializeField] private float squareSize;

    private Token[,] _grid;
    private Token _selectedToken;
    private GameController _controller;
    private SquareSelectorCreator _squareSelector;

    private void Awake()
    {
        _squareSelector = GetComponent<SquareSelectorCreator>();
        CreateGrid();
    }

    public void SetDependencies(GameController gameController)
    {
        _controller = gameController;
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
        TryAttackOpponentToken(coords);
        UpdateBoardOnTokenMove(coords, token.OccupiedSquare, token, null);
        token.MoveToken(coords);
        DeselectToken();
        _controller.UseActivePlayerActionPoint();
    }

    private void TryAttackOpponentToken(Vector2Int coords)
    {
        var token = GetTokenOnSquare(coords);
        if (token && !token.IsFromSameTeam(_selectedToken))
        {
            AttackToken(token);
        }
    }

    private void AttackToken(Token token)
    {
        Debug.Log("Attack not implemented");
    }

    private void TakeToken(Token token)
    {
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