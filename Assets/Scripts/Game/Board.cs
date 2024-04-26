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
    private UIManager _uiManager;
    private SquareSelectorCreator _squareSelector;
    private Vector2Int _lastSelectedSquare;

    private void Awake()
    {
        _squareSelector = GetComponent<SquareSelectorCreator>();
        CreateGrid();
    }

    public void SetDependencies(GameController gameController, UIManager uiManager)
    {
        _controller = gameController;
        _uiManager = uiManager;
        
        _uiManager.moveButtonPressed.AddListener(OnMoveButtonPressed);
        _uiManager.defendButtonPressed.AddListener(OnDefendButtonPressed);
    }

    public void ClearDefendedSquares(Player player)
    {
        _squareSelector.ClearDefendedSquares(player);
    }

    private void OnDefendButtonPressed()
    {
        if (!_controller.IsGameInProgress()) return;

        var token = GetTokenOnSquare(_lastSelectedSquare);

        if (!_selectedToken
            || !_selectedToken.CanMoveTo(_lastSelectedSquare)
            || token)
            return;

        _selectedToken.IsDefending = true;
        var position = CalculatePositionFromCoords(_lastSelectedSquare);
        
        _squareSelector.ShowDefendedSquare(position, _controller.ActivePlayer, _selectedToken);
        OnMoveSelectedToken(_lastSelectedSquare, _selectedToken);
    }

    private void OnMoveButtonPressed()
    {
        if (!_controller.IsGameInProgress()) return;

        Token token = GetTokenOnSquare(_lastSelectedSquare);

        if (!_selectedToken
            || !_selectedToken.CanMoveTo(_lastSelectedSquare)
            || token)
            return;

        OnMoveSelectedToken(_lastSelectedSquare, _selectedToken);
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

    public void OnCancelMove()
    {
        if (!_selectedToken) return;
        
        DeselectToken();
        _controller.UseActivePlayerActionPoint();
        _uiManager.HideActionButtons();
    }

    public void OnSquareHovered(Vector3? inputPosition)
    {
        if (!_controller.IsGameInProgress()) return;
        if (!inputPosition.HasValue) return;
        
        var coords = CalculateCoordsFromPosition(inputPosition.Value);
        var token = GetTokenOnSquare(coords);
        if (token && token != _selectedToken)
        {
            _uiManager.ShowTokenInfoPanel(inputPosition.Value, token.Health, token.Attack, token.Defence);   
        }
        else
        {
            _uiManager.HideTokenInfoPanel();
                
        }
    }

    public void OnSquareSelected(Vector3? inputPosition)
    {
        if (!_controller.IsGameInProgress()) return;
        if (!inputPosition.HasValue) return;
        if (_uiManager.AreActionButtonsActive()) return;
        
        var coords = CalculateCoordsFromPosition(inputPosition.Value);
        var token = GetTokenOnSquare(coords);

        //a token is already selected
        if (_selectedToken)
        {
            //cannot move to square
            if (!_selectedToken.CanMoveTo(coords)) return;

            //opponent on square
            if (HasSquareOpponentToken(coords))
            {
                OnMoveSelectedToken(coords, _selectedToken);
            }
            else //choose to move to or defend square
            {
                _uiManager.ShowActionButtonsAtPosition(inputPosition.Value);
                _lastSelectedSquare = coords;
            }
        }
        else //no currently selected token
        {
            //clicked on token from the same team
            if (token && _controller.IsTeamTurnActive(token.Team) && !token.IsDefending)
            {
                SelectToken(token);
            }
        }
    }

    private void OnMoveSelectedToken(Vector2Int coords, Token token)
    {
        //opponent on selected square
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
        else //no opponent on selected square
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
        var newHealth = token.Health - damage;

        if (token.Health != newHealth)
        {
            token.Health = newHealth;
            _uiManager.UpdateTokenInfoPanel(newHealth, token.Attack, token.Defence);
        }

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
        _controller.SelectToken(token);
        _selectedToken.SelectAvailableSquares();
        var moves = _selectedToken.AvailableMoves;
        ShowAvailableMovesSquares(moves);
    }

    private void ShowAvailableMovesSquares(List<Vector2Int> moves)
    {
        Dictionary<Vector3, bool> squareData = new();

        foreach (var move in moves)
        {
            var position = CalculatePositionFromCoords(move);
            var token = GetTokenOnSquare(move);

            if (token)
            {
                if (!token.IsDefending)
                {
                    squareData.Add(position, false);
                }
            }
            else
            {
                squareData.Add(position, true);
            }
        }

        _squareSelector.ShowAvailableMoves(squareData, _selectedToken);
    }

    private void DeselectToken()
    {
        _controller.DeselectToken(_selectedToken);
        _selectedToken = null;
        _squareSelector.ClearAvailableMoves();
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