namespace InventoryApp.Domain;

/// <summary>One stock item. Money is integer cents. Build it only through <see cref="Create"/>.</summary>
public sealed record Item(
    string Id,
    string Name,
    string Category,
    int Quantity,
    int UnitPriceCents,
    int ReorderLevel)
{
    public const int MaxUnitPriceCents = 10_000_000;

    /// <summary>quantity * price; long because the product can exceed int.</summary>
    public long StockValueCents => (long)Quantity * UnitPriceCents;

    public static Result<Item> Create(string id, string name, string category,
                                      int quantity, int unitPriceCents, int reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Result.Fail<Item>(ErrorCode.InvalidData, "Item id must not be empty.");
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail<Item>(ErrorCode.InvalidData, $"Item '{id}': name must not be empty.");
        if (string.IsNullOrWhiteSpace(category))
            return Result.Fail<Item>(ErrorCode.InvalidData, $"Item '{id}': category must not be empty.");
        if (quantity < 0)
            return Result.Fail<Item>(ErrorCode.InvalidData, $"Item '{id}': quantity must be >= 0 (was {quantity}).");
        if (unitPriceCents < 0 || unitPriceCents > MaxUnitPriceCents)
            return Result.Fail<Item>(ErrorCode.InvalidData,
                $"Item '{id}': unit price must be 0..{MaxUnitPriceCents} cents (was {unitPriceCents}).");
        if (reorderLevel < 0)
            return Result.Fail<Item>(ErrorCode.InvalidData, $"Item '{id}': reorder level must be >= 0 (was {reorderLevel}).");

        return Result.Ok(new Item(id.Trim(), name.Trim(), category.Trim(), quantity, unitPriceCents, reorderLevel));
    }
}