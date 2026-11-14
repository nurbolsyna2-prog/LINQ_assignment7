using System.Globalization;

namespace InventoryApp.Workflow;

/// <summary>Pure text formatting. Money is shown as cents/100 with 2 decimals.</summary>
public static class ReportFormatter
{
    public static string Money(long cents) =>
        (cents / 100m).ToString("N2", CultureInfo.InvariantCulture);

    public static IEnumerable<string> RestockPlanCsv(IEnumerable<RestockLine> plan)
    {
        yield return "id,name,category,order_quantity,order_cost_cents,handling_fee_cents,total_cents,score";
        foreach (var l in plan)
            yield return string.Join(',', l.Row.Id, l.Row.Name, l.Row.Category, l.OrderQuantity,
                                     l.OrderCostCents, l.HandlingFeeCents, l.TotalCents, l.Row.Score);
    }
}