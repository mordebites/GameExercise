using System;
using System.Linq;
using Enums;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(TokenCreator))]
public class GameController : MonoBehaviour
{
    private enum GameState
    {
        Init,
        Play,
        Paused,
        Finished
    }

    private const int MaxActionPointsPerTurn = 4;

    [SerializeField] private BoardLayout startingBoardLayout;
    [SerializeField] private TokenSetStats tokenSetStats;
    [SerializeField] private Board board;
    [SerializeField] UIManager uiManager;
    private TokenCreator _tokenCreator;

    private Player _whitePlayer;
    private Player _blackPlayer;

    private GameState _state;
    private int _turnCounter;
    private int _currentPlayerActionPoints;

    public Player ActivePlayer { get; private set; }

    private void Awake()
    {
        _tokenCreator = GetComponent<TokenCreator>();
        CreatePlayers();
    }

    private void CreatePlayers()
    {
        _whitePlayer = new Player(TeamColour.White, board);
        _blackPlayer = new Player(TeamColour.Black, board);
    }

    void Start()
    {
        StartNewGame();
    }

    private void StartNewGame()
    {
        _state = GameState.Init;
        UpdateTurnCounter(1);
        UpdateActionPoints(MaxActionPointsPerTurn);
        ChangeActiveTeam(_whitePlayer);

        uiManager.endTurnButtonPressed.AddListener(OnEndTurnButtonPressed);
        uiManager.codexToggleChanged.AddListener(OnCodexToggleChanged);
        
        board.SetDependencies(this, uiManager);
        CreateTokensFromLayoutAndStats(startingBoardLayout, tokenSetStats);
        _state = GameState.Play;
    }

    private void OnEndTurnButtonPressed()
    {
        EndTurn();
    }

    private void OnCodexToggleChanged(bool isOn)
    {
        SetGamePaused(isOn);
    }

    private void UpdateActionPoints(int newPoints)
    {
        _currentPlayerActionPoints = newPoints;
        uiManager.UpdateActionPoints(_currentPlayerActionPoints);
    }

    private void UpdateTurnCounter(int newCounter)
    {
        _turnCounter = newCounter;
        uiManager.UpdateTurnCounter(newCounter);
    }

    public bool IsGameInProgress()
    {
        return _state == GameState.Play;
    }

    private void CreateTokensFromLayoutAndStats(BoardLayout boardLayout, TokenSetStats stats)
    {
        for (var i = 0; i < boardLayout.GetTokenCount(); i++)
        {
            var coords = boardLayout.GetSquareCoordsAtIndex(i);
            var type = boardLayout.GetTokenTypeAtIndex(i);
            var colour = boardLayout.GetSquareTeamColourAtIndex(i);

            CreateTokenAndInitialize(coords, type, colour, stats);
        }
    }

    private void CreateTokenAndInitialize(Vector2Int coords, TokenType tokenType, TeamColour team, TokenSetStats stats)
    {
        var newToken = _tokenCreator.CreateToken(tokenType).GetComponent<Token>();
        var tokenStats = stats.GetStatsForType(tokenType);

        if (tokenStats == null)
        {
            Debug.LogError($"Could not find stats for token type: {tokenType}");
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
    Application.Quit();
#endif
        }
        
        newToken.SetData(coords, team, board, tokenStats);

        var teamMaterial = _tokenCreator.GetTeamMaterial(team);
        newToken.SetMaterial(teamMaterial);

        board.SetTokenOnBoard(coords, newToken);

        var player = team == TeamColour.Black ? _blackPlayer : _whitePlayer;
        player.AddToken(newToken);
    }

    public void SetGamePaused(bool shouldPause)
    {
        _state = _state switch
        {
            GameState.Play when shouldPause => GameState.Paused,
            GameState.Paused when !shouldPause => GameState.Play,
            _ => _state
        };
    }

    public void SelectToken(Token token)
    {
        token.SetMaterial(_tokenCreator.GetSelectedTokenMaterial());
    }

    public void DeselectToken(Token token)
    {
        token.SetMaterial(_tokenCreator.GetTeamMaterial(token.Team));
    }

    public bool IsTeamTurnActive(TeamColour tokenTeam)
    {
        return tokenTeam == ActivePlayer.team;
    }

    public void UseActivePlayerActionPoint()
    {
        var points = Math.Max(0, _currentPlayerActionPoints - 1);
        UpdateActionPoints(points);
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
            UpdateTurnCounter(_turnCounter + 1);
            var nextPlayer = GetOpponent(ActivePlayer);
            ChangeActiveTeam(nextPlayer);
            ClearDefendedSquares();
        }
    }

    private void ClearDefendedSquares()
    {
        board.ClearDefendedSquares(ActivePlayer);
        foreach (var token in ActivePlayer.activeTokens)
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

        var winner = _whitePlayer.activeTokens.Count > 0 ? _whitePlayer : _blackPlayer;
        uiManager.ShowWinnerText(winner.team.ToString());
    }

    private void ChangeActiveTeam(Player player)
    {
        ActivePlayer = player;
        UpdateActionPoints(MaxActionPointsPerTurn);
        
        uiManager.UpdateCurrentPlayer(ActivePlayer.team.ToString());
    }

    private Player GetOpponent(Player player)
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