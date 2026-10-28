using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TicTacToe5
{
    public class TicTacToeGame : MonoBehaviour
    {
        const int Size = 5;
        const int WinLength = 5;

        [Header("Board (25 cells, top-left → bottom-right)")]
        public CellView[] cells = new CellView[Size * Size];

        [Header("Sprites")]
        public Sprite playerSprite;
        public Sprite botSprite;

        [Header("Win score (corners)")]
        public Text playerScoreText;
        public Text botScoreText;
        public Text drawScoreText;

        [Header("Status and buttons")]
        public Text statusText;
        public Button newGameButton;
        public Button firstMoveButton;
        public Text firstMoveLabel;
        public Button resetScoreButton;

        [Header("Bot")]
        [Tooltip("Max thinking time per move (s). The search goes as deep as it can in this time.")]
        [Range(0.25f, 10f)] public float botThinkTime = 2f;
        [Tooltip("Minimum pause before the bot moves, so the player can follow the game.")]
        [Range(0f, 2f)] public float botMinDelay = 0.4f;
        [Tooltip("Print the Minimax scores of every move to the Console (the player does not see them).")]
        public bool logMinimaxToConsole = true;

        enum State { PlayerTurn, BotThinking, GameOver }

        Board board;
        State state;
        bool playerFirst = true;
        int playerWins, botWins, draws;
        int gameId;

        void Awake()
        {
            if (playerSprite == null || botSprite == null)
                Debug.LogWarning("[Tic-Tac-Toe] Assign Player Sprite and Bot Sprite on the Game Manager.");
        }

        void Start()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                int index = i;
                cells[i].Index = i;
                cells[i].Button.onClick.AddListener(() => OnCellClicked(index));
            }
            if (newGameButton) newGameButton.onClick.AddListener(NewGame);
            if (firstMoveButton) firstMoveButton.onClick.AddListener(ToggleFirstMove);
            if (resetScoreButton) resetScoreButton.onClick.AddListener(ResetScore);

            UpdateScore();
            UpdateFirstMoveLabel();
            NewGame();
        }

        public void NewGame()
        {
            gameId++;
            board = new Board(Size, WinLength);
            foreach (var c in cells) c.Clear();

            if (playerFirst) BeginPlayerTurn();
            else StartCoroutine(BotTurn(gameId));
        }

        void ToggleFirstMove()
        {
            playerFirst = !playerFirst;
            UpdateFirstMoveLabel();
            NewGame();
        }

        void ResetScore()
        {
            playerWins = botWins = draws = 0;
            UpdateScore();
        }

        void BeginPlayerTurn()
        {
            state = State.PlayerTurn;
            SetStatus("Your turn");
            SetBoardInteractable(true);
        }

        void OnCellClicked(int index)
        {
            if (state != State.PlayerTurn || board.Cells[index] != Player.None) return;

            Place(index, Player.Human);
            if (CheckGameOver()) return;
            StartCoroutine(BotTurn(gameId));
        }

        IEnumerator BotTurn(int id)
        {
            state = State.BotThinking;
            SetStatus("Bot is thinking...");
            SetBoardInteractable(false);

            Board snapshot = board.Clone();
            int timeMs = Mathf.RoundToInt(botThinkTime * 1000f);
            float started = Time.realtimeSinceStartup;
            Task<SearchResult> task = Task.Run(() => new MinimaxAI(Player.AI).FindBestMove(snapshot, 99, timeMs));

            while (!task.IsCompleted || Time.realtimeSinceStartup - started < botMinDelay)
                yield return null;

            if (id != gameId) yield break;

            int move;
            if (task.IsFaulted || task.Result.BestMove < 0)
            {
                if (task.IsFaulted) Debug.LogException(task.Exception);
                move = Enumerable.Range(0, board.CellCount).First(i => board.Cells[i] == Player.None);
            }
            else
            {
                move = task.Result.BestMove;
                if (logMinimaxToConsole) LogSearch(task.Result);
            }

            Place(move, Player.AI);
            if (!CheckGameOver()) BeginPlayerTurn();
        }

        void Place(int index, int player)
        {
            board.Place(index, player);
            cells[index].SetMark(player == Player.Human ? playerSprite : botSprite);
        }

        bool CheckGameOver()
        {
            int winner = board.FindWinner(out int[] line);
            if (winner == Player.None && board.EmptyCount > 0) return false;

            state = State.GameOver;
            SetBoardInteractable(false);

            if (winner == Player.Human) { playerWins++; SetStatus("You win!"); }
            else if (winner == Player.AI) { botWins++; SetStatus("Bot wins"); }
            else { draws++; SetStatus("Draw"); }

            if (line != null)
                foreach (int i in line) cells[i].Highlight.enabled = true;

            UpdateScore();
            return true;
        }

        void SetBoardInteractable(bool on)
        {
            for (int i = 0; i < cells.Length; i++)
                cells[i].Button.interactable = on && board.Cells[i] == Player.None;
        }

        void SetStatus(string text)
        {
            if (statusText) statusText.text = text;
        }

        void UpdateScore()
        {
            if (playerScoreText) playerScoreText.text = $"Player: {playerWins}";
            if (botScoreText) botScoreText.text = $"Bot: {botWins}";
            if (drawScoreText) drawScoreText.text = $"Draws: {draws}";
        }

        void UpdateFirstMoveLabel()
        {
            if (firstMoveLabel) firstMoveLabel.text = playerFirst ? "First move: Player" : "First move: Bot";
        }

        void LogSearch(SearchResult r)
        {
            if (r.RootScores == null) return;
            var moves = Enumerable.Range(0, r.RootScores.Length)
                .Where(i => !float.IsNaN(r.RootScores[i]))
                .OrderByDescending(i => r.RootScores[i])
                .Select(i => $"{board.CellName(i)}={(r.RootIsUpperBound[i] ? "≤" : "")}{r.RootScores[i]:+0.00;-0.00;0.00}");
            Debug.Log($"[Minimax] depth {r.DepthReached}{(r.SolvedExactly ? " (full tree)" : "")}, " +
                      $"{r.Nodes:N0} positions, {r.Milliseconds:0} ms -> plays {board.CellName(r.BestMove)} " +
                      $"(score {r.BestScore:+0.00;-0.00;0.00})\n{string.Join(", ", moves)}");
        }
    }
}
