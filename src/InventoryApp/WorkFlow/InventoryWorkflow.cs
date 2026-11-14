using InventoryApp.Domain;
using InventoryApp.Extensions;
using InventoryApp.Io;
using InventoryApp.Native;
using InventoryApp.Reports;

namespace InventoryApp.Workflow;

public sealed record WorkflowOutput(
    IReadOnlyList<RestockLine> Plan,
    CategoryValueReport Categories,
    string ExportPath);

public static class InventoryWorkflow
{
    /// load snapshot (IO) -> pure reports -> native fee calculation -> export (IO).
    /// The export is written only after every earlier step succeeded.
    public static Result<WorkflowOutput> Run(
        string csvPath, string exportPath, Func<Item, long> score, IFeeCalculator fees) =>
        CsvItemSource.LoadSnapshot(csvPath)
            .Then(items =>
            {
                var rows = InventoryReports.LowStockReport(items, score);
                var categories = InventoryReports.ValueByCategory(items);
                return RestockPlanner.Build(rows, fees)
                    .Then(plan => ReportExporter.WriteLines(exportPath, ReportFormatter.RestockPlanCsv(plan))
                        .Then(path => Result.Ok(new WorkflowOutput(plan, categories, path))));
            });
}