namespace GameCore
{
    // CODEX BOSS PR2
    public enum SpecialType
    {
        None = 0,
        Bomb = 1,
        RowClear = 2,
        ColumnClear = 3,
        ColorBomb = 4,
        TreasureChest = 5, // CODEX CHEST PR1
        Player = 6,
        Item = 7, // CODEX STAGE 7B: board-spawned items.
        Bugada = 8, // CODEX STAGE 7D: Bugada special item.
        Tumor = 9 // CODEX REPLAYABILITY: level mini-goal target special.
    }
}
