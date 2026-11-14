using InventoryApp.Domain;
using InventoryApp.Extensions;

namespace InventoryApp.Reports;

public sealed record LowStockRow(
    string Id, string Name, string Category,
    int Quantity, int ReorderLevel, int Shortfall, int UnitPriceCents, long Score);

public sealed record CategoryTotal(string Category, int ItemCount, long TotalUnits, long TotalValueCents);

public sealed record CategoryValueReport(IReadOnlyList<CategoryTotal> Categories, long GrandTotalCents);

/// <summary>Pure report calculations. No Console, no files, no native calls.</summary>
public static class InventoryReports
{
    /// REPORT A - Low-stock restock priorities (user: warehouse buyer).
    /// Where (LowStock) -> Select -> OrderByDescending(score) -> ThenBy(Id).
    public static IReadOnlyList<LowStockRow> LowStockReport(IEnumerable<Item> items, Func<Item, long> score) =>
        items.LowStock()
             .Select(i => new LowStockRow(
                 i.Id, i.Name, i.Category, i.Quantity, i.ReorderLevel,
                 i.ReorderLevel - i.Quantity, i.UnitPriceCents, score(i)))
             .OrderByDescending(r => r.Score)
             .ThenBy(r => r.Id, StringComparer.Ordinal)      // deterministic tie-breaker
             .ToList();

    /// REPORT B - Stock value by category (user: finance).
    /// GroupBy -> Select (Count, Sum) -> OrderByDescending(value) -> ThenBy(category); grand total with Aggregate.
    public static CategoryValueReport ValueByCategory(IEnumerable<Item> items)
    {
        var categories = items
            .GroupBy(i => i.Category, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CategoryTotal(
                g.Key, g.Count(), g.Sum(i => (long)i.Quantity), g.Sum(i => i.StockValueCents)))
            .OrderByDescending(c => c.TotalValueCents)
            .ThenBy(c => c.Category, StringComparer.Ordinal)
            .ToList();

        long grandTotal = categories.Aggregate(0L, (sum, c) => sum + c.TotalValueCents);
        return new CategoryValueReport(categories, grandTotal);
    }
}