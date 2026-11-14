using InventoryApp.Domain;

namespace InventoryApp.Extensions;

public static class ItemQueryExtensions
{
    /// <summary>
    /// EXTENSION 1 - receiver IEnumerable&lt;Item&gt;, returns IEnumerable&lt;Item&gt;.
    /// Items whose stock is strictly below their reorder level. Deferred, pure, does not modify the source.
    /// It sits on the sequence of items because "low stock" is a business rule reused by several reports.
    /// </summary>
    public static IEnumerable<Item> LowStock(this IEnumerable<Item> items) =>
        items.Where(i => i.Quantity < i.ReorderLevel);
}