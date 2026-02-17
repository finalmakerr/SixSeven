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
        [SerializeField] private float holdToHealDuration = 0.5f;

        private Piece selectedPiece;
        private Vector2 startPosition;
        private bool isHoldingPlayer;
        private float holdStartTime;
        private bool holdTriggered;

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
                isHoldingPlayer = selectedPiece != null && selectedPiece.IsPlayer;
                holdStartTime = Time.time;
                holdTriggered = false;
            }
            else if (Input.GetMouseButton(0) && isHoldingPlayer)
            {
                var currentPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                var delta = currentPosition - startPosition;
                var manager = gameManager != null ? gameManager : GameManager.Instance;

                if (delta.magnitude >= swipeThreshold)
                {
                    isHoldingPlayer = false;
                    if (manager != null)
                    {
                        manager.EndBossPowerAccess();
                    }
                }
                else if (Time.time - holdStartTime >= holdToHealDuration)
                {
                    if (manager != null && manager.HasBossPowersForAccess())
                    {
                        holdTriggered = true;
                        manager.BeginBossPowerAccess();

                        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                        {
                            manager.CycleBossPowerSelection(-1);
                        }

                        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                        {
                            manager.CycleBossPowerSelection(1);
                        }
                    }
                    else if (!holdTriggered && manager != null && manager.TryEnergyHeal())
                    {
                        holdTriggered = true;
                        selectedPiece = null;
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0) && selectedPiece != null)
            {
                var manager = gameManager != null ? gameManager : GameManager.Instance;
                if (holdTriggered && manager != null && manager.IsBossPowerAccessActive)
                {
                    manager.TryUseSelectedBossPower();
                    manager.EndBossPowerAccess();
                }
                else
                {
                    var endPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    var delta = endPosition - startPosition;
                    if (delta.magnitude >= swipeThreshold)
                    {
                        // Only attempt a swap once the pointer has moved far enough.
                        var direction = GetDirection(delta);
                        if (selectedPiece.IsPlayer)
                        {
                            if (manager != null && board.CanJetpackDouble(selectedPiece, direction))
                            {
                                var movementCost = JetpackEnergyCost;
                                if (board.TryGetPieceAt(new Vector2Int(selectedPiece.X, selectedPiece.Y), out var tile)
                                    && tile != null
                                    && tile.GetTileDebuff() == TileDebuffType.Entangled)
                                {
                                    movementCost += 1;
                                }

                                if (manager.CanUseManualAbility() && manager.HasEnoughEnergy(movementCost))
                                {
                                    if (board.TryJetpackDouble(selectedPiece, direction))
                                    {
                                        if (manager.TrySpendEnergy(movementCost))
                                        {
                                            manager.RegisterJetpackMove();
                                            manager.TriggerJetpackDoubleSuccess();
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            board.TrySwap(selectedPiece, direction);
                        }
                    }
                }

                selectedPiece = null;
                isHoldingPlayer = false;
                holdTriggered = false;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                var manager = gameManager != null ? gameManager : GameManager.Instance;
                if (manager != null)
                {
                    manager.EndBossPowerAccess();
                }

                selectedPiece = null;
                isHoldingPlayer = false;
                holdTriggered = false;
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
