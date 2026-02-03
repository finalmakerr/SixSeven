using UnityEngine;

namespace GameCore
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Board board;
        // STAGE 4: Swipe threshold in screen pixels.
        [SerializeField] private float swipeThresholdPixels = 25f;

        private Piece selectedPiece;
        private Piece pressedPiece;
        private Vector2 startScreenPosition;
        private Vector2 startWorldPosition;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (mainCamera == null || board == null)
            {
                selectedPiece = null;
                return;
            }

            // STAGE 0: Disable input while the board is resolving swaps/cascades.
            if (board.IsBusy)
            {
                selectedPiece = null;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                startScreenPosition = Input.mousePosition;
                startWorldPosition = mainCamera.ScreenToWorldPoint(startScreenPosition);
                pressedPiece = GetPieceAtPosition(startWorldPosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HandlePointerRelease();
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
            // STAGE 4: Only allow 4-direction swaps.
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            }

            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        // STAGE 4: Tap-select and swipe handling.
        private void HandlePointerRelease()
        {
            if (mainCamera == null || board == null)
            {
                selectedPiece = null;
                pressedPiece = null;
                return;
            }

            var endScreenPosition = (Vector2)Input.mousePosition;
            var endWorldPosition = mainCamera.ScreenToWorldPoint(endScreenPosition);
            var deltaScreen = endScreenPosition - startScreenPosition;

            if (pressedPiece != null && deltaScreen.magnitude >= swipeThresholdPixels)
            {
                // STAGE 4: Swipe on tile to swap.
                var direction = GetDirection(deltaScreen);
                if (board.TrySwap(pressedPiece, direction))
                {
                    TriggerHaptic();
                }

                selectedPiece = null;
                pressedPiece = null;
                return;
            }

            // STAGE 4: Tap tile A then adjacent tile B to swap.
            var tappedPiece = GetPieceAtPosition(endWorldPosition);
            if (tappedPiece == null)
            {
                selectedPiece = null;
                pressedPiece = null;
                return;
            }

            if (selectedPiece == null)
            {
                selectedPiece = tappedPiece;
            }
            else if (tappedPiece == selectedPiece)
            {
                selectedPiece = null;
            }
            else if (AreAdjacent(selectedPiece, tappedPiece))
            {
                var direction = new Vector2Int(tappedPiece.X - selectedPiece.X, tappedPiece.Y - selectedPiece.Y);
                if (board.TrySwap(selectedPiece, direction))
                {
                    TriggerHaptic();
                }

                selectedPiece = null;
            }
            else
            {
                selectedPiece = tappedPiece;
            }

            pressedPiece = null;
        }

        // STAGE 4: Optional haptic hook.
        private void TriggerHaptic()
        {
        }

        // STAGE 4: Adjacency check for tap-to-swap.
        private bool AreAdjacent(Piece first, Piece second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            return Mathf.Abs(first.X - second.X) + Mathf.Abs(first.Y - second.Y) == 1;
        }
    }
}
