using System;
using System.Collections.Generic;

public sealed class BrainrotShopLogic
{
    public const string OneUpItemId = "1UP";
    public const string LeaveWithoutOneUpWarning = "ARE YOU THAT CRAZY??";
    public const int MaxOneUpStack = 7;

    private readonly BrainrotStarTracker starTracker;
    private readonly HashSet<string> purchasedItems = new();
    private readonly Dictionary<string, int> ownedItemCounts = new();

    public BrainrotShopLogic(BrainrotStarTracker tracker)
    {
        starTracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        starTracker.CoinsChanged += coins => CoinBalanceChanged?.Invoke(coins);
        starTracker.StarsChanged += stars => StarCountChanged?.Invoke(stars);
        starTracker.ChestConsumed += result => ChestConsumed?.Invoke(result);
    }

    public event Action<int> CoinBalanceChanged;
    public event Action<int> StarCountChanged;
    public event Action<BrainrotStarTracker.ChestConsumeResult> ChestConsumed;
    public event Action<string, int> ItemPurchased;
    public event Action<string, int> ItemSold;
    public event Action<string> ShopWarningRequested;

    public int getPlayerCoins() => starTracker.getPlayerCoins();

    public void addCoins(int amount) => starTracker.addCoins(amount);

    public bool spendCoins(int itemCost) => starTracker.spendCoins(itemCost);

    public bool PurchaseItem(string itemId, int itemCost)
    {
        if (string.IsNullOrWhiteSpace(itemId) || itemCost <= 0)
            return false;

        if (!spendCoins(itemCost))
            return false;

        purchasedItems.Add(itemId);
        IncrementOwnedItemCount(itemId);
        ItemPurchased?.Invoke(itemId, itemCost);
        return true;
    }

    public bool PurchaseOneUp(int itemCost)
    {
        if (starTracker.ExtraLives >= MaxOneUpStack)
            return false;

        if (!PurchaseItem(OneUpItemId, itemCost))
            return false;

        starTracker.AddExtraLife(1);
        return true;
    }

    public bool TrySellItem(string itemId, int sellValue)
    {
        if (string.IsNullOrWhiteSpace(itemId) || sellValue <= 0)
            return false;

        if (string.Equals(itemId, OneUpItemId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!TryDecrementOwnedItemCount(itemId))
            return false;

        if (GetOwnedItemCount(itemId) <= 0)
            purchasedItems.Remove(itemId);

        addCoins(sellValue);
        ItemSold?.Invoke(itemId, sellValue);

        return true;
    }

    public bool ShouldOfferOneUpOnResurrection() => true;

    public bool TryLeaveShop()
    {
        if (starTracker.ExtraLives > 0)
            return true;

        ShopWarningRequested?.Invoke(LeaveWithoutOneUpWarning);
        return true;
    }

    public bool ShouldSpawnOneUpInNormalShop()
    {
        return UnityEngine.Random.Range(0f, 100f) <= GetShopSpawnChancePercent();
    }

    public float GetShopSpawnChancePercent()
    {
        var lives = Math.Max(0, starTracker.ExtraLives);
        var chance = 100f - (20f * lives);
        return Math.Max(40f, chance);
    }

    public bool HasItem(string itemId) => purchasedItems.Contains(itemId);

    public int GetOwnedItemCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return 0;

        return ownedItemCounts.TryGetValue(itemId, out var count) ? count : 0;
    }

    private void IncrementOwnedItemCount(string itemId)
    {
        if (ownedItemCounts.TryGetValue(itemId, out var count))
            ownedItemCounts[itemId] = count + 1;
        else
            ownedItemCounts[itemId] = 1;
    }

    private bool TryDecrementOwnedItemCount(string itemId)
    {
        if (!ownedItemCounts.TryGetValue(itemId, out var count) || count <= 0)
            return false;

        if (count == 1)
            ownedItemCounts.Remove(itemId);
        else
            ownedItemCounts[itemId] = count - 1;

        return true;
    }

    public bool triggerChestConsume() => starTracker.triggerChestConsume();

    public int updateStarCount() => starTracker.updateStarCount();
}
