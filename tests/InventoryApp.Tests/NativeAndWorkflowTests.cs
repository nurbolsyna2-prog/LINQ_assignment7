using System.Runtime.InteropServices;
using InventoryApp.Domain;
using InventoryApp.Native;
using InventoryApp.Scoring;
using InventoryApp.Workflow;

namespace InventoryApp.Tests;

/// <summary>Bindings that point at things that do not exist: they cause REAL load/entry-point failures.</summary>
internal static class BrokenBindings
{
    [DllImport("no_such_native_library_xyz", EntryPoint = "inv_handling_fee_cents")]
    internal static extern int MissingLibrary(int unitPriceCents, int quantity);

    [DllImport("inventory_native", EntryPoint = "inv_no_such_function")]
    internal static extern int MissingEntryPoint(int unitPriceCents, int quantity);
}

internal sealed class FailingFees : IFeeCalculator
{
    public Result<int> HandlingFeeCents(int unitPriceCents, int quantity) =>
        Result.Fail<int>(ErrorCode.NativeLoadFailed, "injected failure");
}

public class NativeAndWorkflowTests
{
    [Fact]
    public void RealNativeCall_ReturnsManuallyCalculatedFee()
    {
        var fees = new NativeFeeCalculator();   // real P/Invoke into libinventory_native

        Assert.Equal(375, fees.HandlingFeeCents(1250, 15).Value);    // 18750 * 2% = 375
        Assert.Equal(1494, fees.HandlingFeeCents(24900, 3).Value);   // 74700 * 2% = 1494
    }

    [Fact]
    public void RealNativeCall_AppliesMinimumCapAndZeroQuantityRules()
    {
        var fees = new NativeFeeCalculator();

        Assert.Equal(150, fees.HandlingFeeCents(100, 1).Value);                 // 2 -> minimum 150
        Assert.Equal(50_000, fees.HandlingFeeCents(10_000_000, 100_000).Value); // cap
        Assert.Equal(0, fees.HandlingFeeCents(1250, 0).Value);                  // quantity 0 -> no fee
        Assert.Equal(150, fees.HandlingFeeCents(7475, 1).Value);                // 149.5 -> 150
    }

    [Fact]
    public void Adapter_RejectsInvalidDomainInput_WithoutCallingNative()
    {
        int calls = 0;
        var fees = new NativeFeeCalculator((_, _) => { calls++; return 0; });

        Assert.Equal(ErrorCode.InvalidData, fees.HandlingFeeCents(-1, 1).Error.Code);
        Assert.Equal(ErrorCode.InvalidData, fees.HandlingFeeCents(Item.MaxUnitPriceCents + 1, 1).Error.Code);
        Assert.Equal(ErrorCode.InvalidData, fees.HandlingFeeCents(1, NativeFeeCalculator.MaxQuantity + 1).Error.Code);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Adapter_NativeErrorCode_BecomesNativeRejectedInput() =>
        Assert.Equal(ErrorCode.NativeRejectedInput, new NativeFeeCalculator((_, _) => -2).HandlingFeeCents(1, 1).Error.Code);

    [Fact]
    public void Adapter_MissingLibrary_BecomesNativeLoadFailed_FromARealLoadAttempt()
    {
        var r = new NativeFeeCalculator(BrokenBindings.MissingLibrary).HandlingFeeCents(1250, 15);

        Assert.False(r.IsSuccess);
        Assert.Equal(ErrorCode.NativeLoadFailed, r.Error.Code);
    }

    [Fact]
    public void Adapter_MissingExport_BecomesNativeBindingFailed_FromARealLookup()
    {
        var r = new NativeFeeCalculator(BrokenBindings.MissingEntryPoint).HandlingFeeCents(1250, 15);

        Assert.False(r.IsSuccess);
        Assert.Equal(ErrorCode.NativeBindingFailed, r.Error.Code);
    }

    [Fact]
    public void Workflow_NormalPath_RunsBothReports_CallsNative_AndExports()
    {
        using var dir = new TempDir();
        var input = dir.Write("in.csv", Samples.Csv);
        var output = dir.File("plan.csv");

        var r = InventoryWorkflow.Run(input, output, ItemScoring.ByShortage(), new NativeFeeCalculator());

        Assert.True(r.IsSuccess);
        Assert.Equal(new[] { "S301", "E200", "A100", "A101", "E202" }, r.Value.Plan.Select(l => l.Row.Id));
        Assert.Equal(new[] { 150, 1494, 375, 630, 400 }, r.Value.Plan.Select(l => l.HandlingFeeCents));
        Assert.Equal(233400L, r.Value.Categories.GrandTotalCents);

        var lines = File.ReadAllLines(output);
        Assert.Equal(6, lines.Length);
        Assert.Equal("S301,Gel Pen Blue,Stationery,32,4800,150,4950,321", lines[1]);
        Assert.False(File.Exists(output + ".tmp"));
    }

    [Fact]
    public void Workflow_NativeFailure_ReportsTheError_AndLeavesPreviousExportUnchanged()
    {
        using var dir = new TempDir();
        var input = dir.Write("in.csv", Samples.Csv);
        var output = dir.Write("plan.csv", "PREVIOUS EXPORT");

        var r = InventoryWorkflow.Run(input, output, ItemScoring.ByShortage(), new FailingFees());

        Assert.False(r.IsSuccess);
        Assert.Equal(ErrorCode.NativeLoadFailed, r.Error.Code);
        Assert.Equal("PREVIOUS EXPORT", File.ReadAllText(output));
        Assert.False(File.Exists(output + ".tmp"));
    }

    [Fact]
    public void Workflow_MissingOrMalformedInput_FailsAndWritesNothing()
    {
        using var dir = new TempDir();
        var output = dir.File("plan.csv");

        var missing = InventoryWorkflow.Run(dir.File("nope.csv"), output, ItemScoring.ByShortage(), new NativeFeeCalculator());
        var bad = InventoryWorkflow.Run(dir.Write("bad.csv", "wrong header\n"), output, ItemScoring.ByShortage(), new NativeFeeCalculator());

        Assert.Equal(ErrorCode.FileNotFound, missing.Error.Code);
        Assert.Equal(ErrorCode.MalformedContent, bad.Error.Code);
        Assert.False(File.Exists(output));
    }

    [Fact]
    public void Workflow_TheTwoConfiguredFunctions_ProduceDifferentPlansFromTheSameFile()
    {
        using var dir = new TempDir();
        var input = dir.Write("in.csv", Samples.Csv);

        var a = InventoryWorkflow.Run(input, dir.File("a.csv"), ItemScoring.ByShortage(), new NativeFeeCalculator()).Value;
        var b = InventoryWorkflow.Run(input, dir.File("b.csv"), ItemScoring.ByValue(), new NativeFeeCalculator()).Value;

        Assert.Equal("S301", a.Plan[0].Row.Id);
        Assert.Equal("E200", b.Plan[0].Row.Id);
    }
}