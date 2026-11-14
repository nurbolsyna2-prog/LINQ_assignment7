using InventoryApp.Domain;
using InventoryApp.Extensions;
using InventoryApp.Native;
using InventoryApp.Reports;

namespace InventoryApp.Workflow;

public sealed record RestockLine(LowStockRow Row, int OrderQuantity, long OrderCostCents, int HandlingFeeCents)
{
    public long TotalCents => OrderCostCents + HandlingFeeCents;
}

public static class RestockPlanner
{
    /// Order quantity = shortfall; the handling fee comes from the injected calculator (native C in production).
    /// The first failure stops the whole plan.
    public static Result<IReadOnlyList<RestockLine>> Build(IEnumerable<LowStockRow> rows, IFeeCalculator fees) =>
        rows.Select(r => fees.HandlingFeeCents(r.UnitPriceCents, r.Shortfall)
                             .Then(fee => Result.Ok(new RestockLine(r, r.Shortfall, (long)r.Shortfall * r.UnitPriceCents, fee))))
            .Sequence();
}