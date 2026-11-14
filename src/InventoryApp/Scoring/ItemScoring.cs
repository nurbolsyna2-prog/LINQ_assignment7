using InventoryApp.Domain;

namespace InventoryApp.Scoring;

public static class ItemScoring
{
    /// <summary>
    /// CURRIED function:  shortfallWeight -> valueWeight -> (Item -> score)
    ///   score = shortfall * shortfallWeight + (unitPriceCents / 100) * valueWeight
    /// </summary>
    public static readonly Func<long, Func<long, Func<Item, long>>> CreateScore =
        shortfallWeight => valueWeight => item =>
        {
            long shortfall = Math.Max(0, item.ReorderLevel - item.Quantity);
            return shortfall * shortfallWeight + (item.UnitPriceCents / 100) * valueWeight;
        };

    // PARTIAL APPLICATION: fixing both configuration arguments yields Item -> score.
    public static Func<Item, long> ByShortage() => CreateScore(10)(1);
    public static Func<Item, long> ByValue() => CreateScore(1)(10);
}