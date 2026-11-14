using InventoryApp.Extensions;
using InventoryApp.Io;
using InventoryApp.Reports;
using InventoryApp.Scoring;

namespace InventoryApp.Tests;

public class ScoringAndDeferredTests
{
    [Fact]
    public void PartiallyApplied_ByShortage_And_ByValue_GiveDifferentResultsOnTheSameItem()
    {
        var item = Samples.I("X", 5, 4500, 10);   // shortfall 5, price 45.00

        long shortage = ItemScoring.ByShortage()(item);   // 5*10 + 45*1  = 95
        long value = ItemScoring.ByValue()(item);         // 5*1  + 45*10 = 455

        Assert.Equal(95L, shortage);
        Assert.Equal(455L, value);
    }

    [Fact]
    public void CurriedFunction_FullApplicationEqualsPartialApplication()
    {
        var item = Samples.I("X", 5, 4500, 10);
        var custom = ItemScoring.CreateScore(3)(2);
        Assert.Equal(5 * 3 + 45 * 2L, custom(item));
    }

    [Fact]
    public void ConfiguredFunctions_ChangeTheTopRowOfTheSameDataset()
    {
        var items = Samples.Items();
        Assert.Equal("S301", InventoryReports.LowStockReport(items, ItemScoring.ByShortage())[0].Id);
        Assert.Equal("E200", InventoryReports.LowStockReport(items, ItemScoring.ByValue())[0].Id);
    }

    [Fact]
    public void Stream_CreatingTheSequenceDoesNotOpenTheSource_IterationDoes()
    {
        int opens = 0;
        var sequence = CsvItemSource.Stream(() => { opens++; return new StringReader(Samples.Csv); });

        Assert.Equal(0, opens);                 // created, body has NOT run

        var first = sequence.First();           // iteration triggers processing
        Assert.Equal(1, opens);
        Assert.True(first.IsSuccess);
        Assert.Equal("A100", first.Value.Id);
    }

    [Fact]
    public void Stream_ProcessesRowsOneByOne_AndKeepsReaderOpenUntilIterationEnds()
    {
        var reader = new TrackingReader(Samples.Csv);
        using (var e = CsvItemSource.Stream(() => reader).GetEnumerator())
        {
            Assert.True(e.MoveNext());
            Assert.False(reader.Disposed);      // mid-iteration: still open
        }
        Assert.True(reader.Disposed);           // enumerator disposed -> released
    }

    [Fact]
    public void WithoutMaterialisation_EachEnumerationReopensTheSource_WithSnapshotItDoesNot()
    {
        int opens = 0;
        Func<TextReader> open = () => { opens++; return new StringReader(Samples.Csv); };

        var deferred = CsvItemSource.Stream(open);
        _ = deferred.Count();
        _ = deferred.Count();
        Assert.Equal(2, opens);                 // two enumerations = two reads

        opens = 0;
        var snapshot = CsvItemSource.Stream(open).Sequence().Value;
        Assert.Equal(1, opens);
        _ = snapshot.Count;
        _ = snapshot.Sum(i => i.Quantity);
        _ = snapshot.ToList();
        Assert.Equal(1, opens);                 // snapshot reuse: no more reads
        Assert.Equal(9, snapshot.Count);
    }
}