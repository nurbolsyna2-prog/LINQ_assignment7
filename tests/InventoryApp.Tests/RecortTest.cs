using InventoryApp.Reports;
using InventoryApp.Scoring;

namespace InventoryApp.Tests;

public class ReportTests
{
    [Fact]
    public void LowStockReport_NormalData_FiltersProjectsAndOrdersByScore()
    {
        var rows = InventoryReports.LowStockReport(Samples.Items(), ItemScoring.ByShortage());

        Assert.Equal(new[] { "S301", "E200", "A100", "A101", "E202" }, rows.Select(r => r.Id));
        Assert.Equal(new long[] { 321, 279, 162, 115, 105 }, rows.Select(r => r.Score));
        Assert.Equal(32, rows[0].Shortfall);           // 40 - 8
    }

    [Fact]
    public void LowStockReport_EmptyInput_ReturnsEmpty() =>
        Assert.Empty(InventoryReports.LowStockReport(new List<Domain.Item>(), ItemScoring.ByShortage()));

    [Fact]
    public void LowStockReport_NoMatchingRecords_ReturnsEmpty()
    {
        var items = new[] { Samples.I("A", 10, 100, 10), Samples.I("B", 50, 100, 10) };   // qty >= reorder
        Assert.Empty(InventoryReports.LowStockReport(items, ItemScoring.ByShortage()));
    }

    [Fact]
    public void LowStockReport_TiedScores_AreOrderedByIdRegardlessOfInputOrder()
    {
        var y = Samples.I("Y", 0, 100, 10);
        var x = Samples.I("X", 0, 100, 10);   // same score as Y

        var one = InventoryReports.LowStockReport(new[] { y, x }, ItemScoring.ByShortage());
        var two = InventoryReports.LowStockReport(new[] { x, y }, ItemScoring.ByShortage());

        Assert.Equal(one[0].Score, one[1].Score);
        Assert.Equal(new[] { "X", "Y" }, one.Select(r => r.Id));
        Assert.Equal(new[] { "X", "Y" }, two.Select(r => r.Id));
    }

    [Fact]
    public void ValueByCategory_GroupsAndTotalsCorrectly()
    {
        var report = InventoryReports.ValueByCategory(Samples.Items());

        Assert.Equal(new[] { "Electronics", "Accessories", "Stationery" }, report.Categories.Select(c => c.Category));
        Assert.Equal(new long[] { 130600, 51750, 51050 }, report.Categories.Select(c => c.TotalValueCents));
        Assert.Equal(new long[] { 18, 48, 123 }, report.Categories.Select(c => c.TotalUnits));
        Assert.Equal(new[] { 3, 3, 3 }, report.Categories.Select(c => c.ItemCount));
        Assert.Equal(233400, report.GrandTotalCents);
    }

    [Fact]
    public void ValueByCategory_EmptyInput_HasNoCategoriesAndZeroTotal()
    {
        var report = InventoryReports.ValueByCategory(new List<Domain.Item>());
        Assert.Empty(report.Categories);
        Assert.Equal(0, report.GrandTotalCents);
    }

    [Fact]
    public void ValueByCategory_SameCategoryDifferentCase_IsOneGroup_AndEqualValuesSortByName()
    {
        var items = new[]
        {
            Samples.I("1", 1, 100, 0, "tools"),
            Samples.I("2", 1, 100, 0, "Tools"),   // same group as "tools"
            Samples.I("3", 2, 100, 0, "Zeta"),    // 200 == tools total 200 -> tie
        };
        var report = InventoryReports.ValueByCategory(items);

        Assert.Equal(2, report.Categories.Count);
        // Both groups total 200 cents -> tie broken by name, ordinal: 'Z' (90) sorts before 't' (116).
        Assert.Equal(new[] { "Zeta", "tools" }, report.Categories.Select(c => c.Category));
        Assert.Equal(new[] { 1, 2 }, report.Categories.Select(c => c.ItemCount));
    }
}