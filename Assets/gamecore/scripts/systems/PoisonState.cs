using UnityEngine;

namespace GameCore.PoisonSystem
{
    public class PoisonState
    {
        public bool IsDormant { get; private set; } = true;
        public bool IsSpreadingRow { get; private set; }
        public bool IsWaitingForNextRise { get; private set; }

        public int CurrentRow { get; private set; } = -1; // -1 means not started
        public int CurrentSpreadCount { get; private set; }
        public int BoardWidth { get; private set; }

        public int TurnsUntilNextAction { get; private set; }

        public bool SpreadFromLeft { get; private set; }

        public void Initialize(int boardWidth, int initialDelay)
        {
            BoardWidth = boardWidth;
            TurnsUntilNextAction = initialDelay;
            IsDormant = true;
            IsSpreadingRow = false;
            IsWaitingForNextRise = false;
            CurrentRow = -1;
            CurrentSpreadCount = 0;
        }

        public void StartFirstRow(int bottomRowIndex, bool spreadFromLeft)
        {
            IsDormant = false;
            IsSpreadingRow = true;
            IsWaitingForNextRise = false;

            CurrentRow = bottomRowIndex;
            CurrentSpreadCount = 0;
            SpreadFromLeft = spreadFromLeft;
        }

        public void AdvanceSpread(int spreadPerTurn)
        {
            CurrentSpreadCount += spreadPerTurn;

            if (CurrentSpreadCount >= BoardWidth)
            {
                CurrentSpreadCount = BoardWidth;
                IsSpreadingRow = false;
                IsWaitingForNextRise = true;
            }
        }

        public void PrepareNextRow(int nextRowIndex, bool spreadFromLeft)
        {
            CurrentRow = nextRowIndex;
            CurrentSpreadCount = 0;
            SpreadFromLeft = spreadFromLeft;
            IsSpreadingRow = true;
            IsWaitingForNextRise = false;
        }

        public void TickTimer()
        {
            if (TurnsUntilNextAction > 0)
                TurnsUntilNextAction--;
        }

        public void ResetTimer(int turns)
        {
            TurnsUntilNextAction = turns;
        }
    }
}
