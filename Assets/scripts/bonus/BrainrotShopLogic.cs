using System;
using System.Collections.Generic;

public sealed class BrainrotShopLogic
{
    private readonly BrainrotStarTracker starTracker;
    private readonly HashSet<string> purchasedItems = new();

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
        ItemPurchased?.Invoke(itemId, itemCost);
        return true;
    }

    public bool HasItem(string itemId) => purchasedItems.Contains(itemId);

    public bool triggerChestConsume() => starTracker.triggerChestConsume();

    public int updateStarCount() => starTracker.updateStarCount();
}
