using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private int startingMoves = 30;
        [SerializeField] private int scorePerPiece = 10;

        [Header("UI")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text movesText;

        public static GameManager Instance { get; private set; }

        public int MovesRemaining { get; private set; }
        public int Score { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            ResetGame();
        }

        public void ResetGame()
        {
            Score = 0;
            MovesRemaining = startingMoves;
            UpdateUI();
        }

        public void AddScore(int piecesCleared)
        {
            Score += piecesCleared * scorePerPiece;
            UpdateUI();
        }

        public bool TryUseMove()
        {
            if (MovesRemaining <= 0)
            {
                return false;
            }

            MovesRemaining -= 1;
            UpdateUI();
            return true;
        }

        private void UpdateUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {Score}";
            }

            if (movesText != null)
            {
                movesText.text = $"Moves: {MovesRemaining}";
            }
        }
    }
}
