using System;
using System.Linq;
using Enums;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(TokenCreator))]
public class GameController : MonoBehaviour
{
    private enum GameState
    {
        Init,
        Play,
        Finished
    }

    private const int MaxActionPointsPerTurn = 4;

    [SerializeField] private BoardLayout startingBoardLayout;
    [SerializeField] private Board board;
    private UIManagerScript _uiManager;
    private TokenCreator _tokenCreator;

    private Player _whitePlayer;
    private Player _blackPlayer;

    private GameState _state;
    private int _turnCounter;
    private int _currentPlayerActionPoints;

    public Player activePlayer { get; private set; }

    private void Awake()
    {
        _tokenCreator = GetComponent<TokenCreator>();
        CreatePlayers();
    }

    private void CreatePlayers()
    {
        _whitePlayer = new Player(TeamColour.White, board);
        _blackPlayer = new Player(TeamColour.Black, board);

        var uiManagerObject = gameObject.scene.GetRootGameObjects()
            .First(o => o.name == "UIManager");
        _uiManager = uiManagerObject.GetComponent<UIManagerScript>();
    }

    void Start()
    {
        StartNewGame();
    }

    private void StartNewGame()
    {
        _state = GameState.Init;
        _turnCounter = 1;
        _currentPlayerActionPoints = MaxActionPointsPerTurn;
        activePlayer = _whitePlayer;

        board.SetDependencies(this, _uiManager);
        CreateTokensFromLayout(startingBoardLayout);
        _state = GameState.Play;
    }

    public bool IsGameInProgress()
    {
        return _state == GameState.Play;
    }

    private void CreateTokensFromLayout(BoardLayout boardLayout)
    {
        for (var i = 0; i < boardLayout.GetTokenCount(); i++)
        {
            var coords = boardLayout.GetSquareCoordsAtIndex(i);
            var typeName = boardLayout.GetTokenNameAtIndex(i);
            var colour = boardLayout.GetSquareTeamColourAtIndex(i);
            var type = Type.GetType(typeName);

            CreateTokenAndInitialize(coords, type, colour);
        }
    }

    private void CreateTokenAndInitialize(Vector2Int coords, Type type, TeamColour team)
    {
        var newToken = _tokenCreator.CreateToken(type).GetComponent<Token>();
        newToken.SetData(coords, team, board);

        var teamMaterial = _tokenCreator.GetTeamMaterial(team);
        newToken.SetMaterial(teamMaterial);

        board.SetTokenOnBoard(coords, newToken);

        var player = team == TeamColour.Black ? _blackPlayer : _whitePlayer;
        player.AddToken(newToken);
    }

    public bool IsTeamTurnActive(TeamColour tokenTeam)
    {
        return tokenTeam == activePlayer.team;
    }

    public void UseActivePlayerActionPoint()
    {
        _currentPlayerActionPoints = Math.Max(0, _currentPlayerActionPoints - 1);
        if (_currentPlayerActionPoints == 0)
        {
            EndTurn();
        }
    }

    public void EndTurn()
    {
        if (IsGameFinished())
        {
            EndGame();
        }
        else
        {
            _turnCounter++;
            var nextPlayer = GetOpponent(activePlayer);
            ChangeActiveTeam(nextPlayer);
            ClearDefendedSquares();
        }
    }

    private void ClearDefendedSquares()
    {
        board.ClearDefendedSquares(activePlayer);
        foreach (var token in activePlayer.activeTokens)
        {
            token.IsDefending = false;
        }
    }

    private bool IsGameFinished()
    {
        return _whitePlayer.activeTokens.Count == 0
               || _blackPlayer.activeTokens.Count == 0;
    }

    private void EndGame()
    {
        _state = GameState.Finished;
    }

    private void ChangeActiveTeam(Player player)
    {
        activePlayer = player;
        _currentPlayerActionPoints = MaxActionPointsPerTurn;
    }

    public Player GetOpponent(Player player)
    {
        return player == _whitePlayer ? _blackPlayer : _whitePlayer;
    }

    public void OnTokenRemoved(Token token)
    {
        var player = token.Team == TeamColour.Black ? _blackPlayer : _whitePlayer;
        player.RemoveToken(token);
        Destroy(token.gameObject);
    }
}