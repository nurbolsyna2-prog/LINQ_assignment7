using InventoryApp.Domain;
using InventoryApp.Native;
using InventoryApp.Scoring;
using InventoryApp.Workflow;

// Usage: InventoryApp [csvPath] [exportPath] [--score shortage|value] [--simulate-native-failure]
var positional = new List<string>();
string scoreName = "shortage";
bool simulateNativeFailure = false;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--score" && i + 1 < args.Length) scoreName = args[++i];
    else if (args[i] == "--simulate-native-failure") simulateNativeFailure = true;
    else positional.Add(args[i]);
}

var csvPath = positional.Count > 0 ? positional[0] : "data/inventory.csv";
var exportPath = positional.Count > 1 ? positional[1] : "restock_plan.csv";

Func<Item, long> score = scoreName.Equals("value", StringComparison.OrdinalIgnoreCase)
    ? ItemScoring.ByValue()          // partially applied function #2
    : ItemScoring.ByShortage();      // partially applied function #1

IFeeCalculator fees = simulateNativeFailure
    ? new NativeFeeCalculator((_, _) => throw new DllNotFoundException("simulated: libinventory_native not found"))
    : new NativeFeeCalculator();

var result = InventoryWorkflow.Run(csvPath, exportPath, score, fees);
if (!result.IsSuccess)
{
    Console.Error.WriteLine($"FAILED [{result.Error.Code}] {result.Error.Message}");
    Console.Error.WriteLine("No export written; previous output (if any) is unchanged.");
    return 1;
}

var output = result.Value;
Console.WriteLine($"== Report A: restock plan (score = {scoreName.ToLowerInvariant()}) ==");
Console.WriteLine($"{"Id",-6}{"Name",-16}{"Reorder",8}{"Order",7}{"Cost",12}{"Fee(native)",13}{"Score",8}");
foreach (var l in output.Plan)
    Console.WriteLine($"{l.Row.Id,-6}{l.Row.Name,-16}{l.Row.ReorderLevel,8}{l.OrderQuantity,7}" +
                      $"{ReportFormatter.Money(l.OrderCostCents),12}{ReportFormatter.Money(l.HandlingFeeCents),13}{l.Row.Score,8}");

Console.WriteLine();
Console.WriteLine("== Report B: stock value by category ==");
foreach (var c in output.Categories.Categories)
    Console.WriteLine($"{c.Category,-14} items={c.ItemCount}  units={c.TotalUnits}  value={ReportFormatter.Money(c.TotalValueCents)}");
Console.WriteLine($"{"TOTAL",-14} value={ReportFormatter.Money(output.Categories.GrandTotalCents)}");

Console.WriteLine();
Console.WriteLine($"Exported: {output.ExportPath}");
return 0;