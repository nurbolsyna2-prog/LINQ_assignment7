using InventoryApp.Domain;
using InventoryApp.Extensions;

namespace InventoryApp.Tests;

public class DomainAndExtensionTests
{
    [Fact]
    public void ItemCreate_ValidValues_TrimsAndComputesStockValue()
    {
        var r = Item.Create(" A1 ", " Cable ", " Tools ", 3, 1250, 5);
        Assert.True(r.IsSuccess);
        Assert.Equal("A1", r.Value.Id);
        Assert.Equal("Tools", r.Value.Category);
        Assert.Equal(3750L, r.Value.StockValueCents);
    }

    [Fact]
    public void ItemCreate_InvalidValues_FailWithInvalidData()
    {
        Assert.Equal(ErrorCode.InvalidData, Item.Create("", "n", "c", 1, 1, 1).Error.Code);
        Assert.Equal(ErrorCode.InvalidData, Item.Create("A", "n", "c", -1, 1, 1).Error.Code);
        Assert.Equal(ErrorCode.InvalidData, Item.Create("A", "n", "c", 1, Item.MaxUnitPriceCents + 1, 1).Error.Code);
        Assert.Equal(ErrorCode.InvalidData, Item.Create("A", "n", "c", 1, 1, -1).Error.Code);
    }

    [Fact]
    public void StockValue_LargeValues_DoNotOverflowInt()
    {
        var item = Samples.I("BIG", 100_000, Item.MaxUnitPriceCents, 0);
        Assert.Equal(1_000_000_000_000L, item.StockValueCents);
    }

    [Fact]
    public void LowStock_ReturnsOnlyItemsStrictlyBelowReorderLevel_AndDoesNotChangeSource()
    {
        var source = new List<Item> { Samples.I("A", 5, 100, 10), Samples.I("B", 10, 100, 10), Samples.I("C", 11, 100, 10) };

        var low = source.LowStock().ToList();

        Assert.Equal(new[] { "A" }, low.Select(i => i.Id));   // equal to the level (B) is NOT low
        Assert.Equal(3, source.Count);
    }

    [Fact]
    public void LowStock_EmptyInput_ReturnsEmpty() =>
        Assert.Empty(new List<Item>().LowStock());

    [Fact]
    public void Then_OnSuccess_RunsNextWithTheValue()
    {
        var r = Result.Ok(20).Then(x => Result.Ok(x + 1));
        Assert.True(r.IsSuccess);
        Assert.Equal(21, r.Value);
    }

    [Fact]
    public void Then_OnFailure_PassesErrorThroughAndNeverCallsNext()
    {
        bool called = false;
        var failed = Result.Fail<int>(ErrorCode.FileNotFound, "missing");

        var r = failed.Then(x => { called = true; return Result.Ok(x); });

        Assert.False(called);
        Assert.False(r.IsSuccess);
        Assert.Equal(ErrorCode.FileNotFound, r.Error.Code);
        Assert.Equal("missing", r.Error.Message);
    }

    [Fact]
    public void Sequence_StopsAtFirstFailure_AndDoesNotEnumerateTheRest()
    {
        int produced = 0;
        IEnumerable<Result<int>> Source()
        {
            produced++; yield return Result.Ok(1);
            produced++; yield return Result.Fail<int>(ErrorCode.MalformedContent, "bad");
            produced++; yield return Result.Ok(3);
        }

        var r = Source().Sequence();

        Assert.False(r.IsSuccess);
        Assert.Equal(ErrorCode.MalformedContent, r.Error.Code);
        Assert.Equal(2, produced);
    }
}