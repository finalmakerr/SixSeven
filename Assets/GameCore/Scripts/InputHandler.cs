using UnityEngine;

namespace GameCore
{
    public class InputHandler : MonoBehaviour
    {
        private const int JetpackEnergyCost = 1;

        [SerializeField] private Camera mainCamera;
        [SerializeField] private Board board;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private float swipeThreshold = 0.2f;

        private Piece selectedPiece;
        private Vector2 startPosition;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Start()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }
        }

        private void Update()
        {
            if (mainCamera == null || board == null)
            {
                selectedPiece = null;
                return;
            }

            if (board.IsBusy)
            {
                selectedPiece = null;
                return; // CODEX VERIFY: lock input while board resolves.
            }

            if (Input.GetMouseButtonDown(0))
            {
                startPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                selectedPiece = GetPieceAtPosition(startPosition);
            }
            else if (Input.GetMouseButtonUp(0) && selectedPiece != null)
            {
                var endPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                var delta = endPosition - startPosition;
                if (delta.magnitude >= swipeThreshold)
                {
                    // Only attempt a swap once the pointer has moved far enough.
                    var direction = GetDirection(delta);
                    if (selectedPiece.IsPlayer)
                    {
                        var manager = gameManager != null ? gameManager : GameManager.Instance;
                        if (manager != null && board.CanJetpackDouble(selectedPiece, direction))
                        {
                            if (manager.TrySpendEnergy(JetpackEnergyCost))
                            {
                                manager.CancelMeditation();
                                board.TryJetpackDouble(selectedPiece, direction);
                            }
                        }
                    }
                    else
                    {
                        board.TrySwap(selectedPiece, direction);
                    }
                }

                selectedPiece = null;
            }
        }

        private Piece GetPieceAtPosition(Vector2 position)
        {
            var hit = Physics2D.Raycast(position, Vector2.zero);
            if (hit.collider != null)
            {
                return hit.collider.GetComponent<Piece>();
            }

            return null;
        }

        private Vector2Int GetDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            }

            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
    }
}
