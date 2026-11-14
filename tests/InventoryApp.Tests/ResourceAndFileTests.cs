using InventoryApp.Domain;
using InventoryApp.Extensions;
using InventoryApp.Io;

namespace InventoryApp.Tests;

public class ResourceAndFileTests
{
    [Fact]
    public void Reader_IsDisposed_AfterSuccessfulFullEnumeration()
    {
        var reader = new TrackingReader(Samples.Csv);
        var result = CsvItemSource.Stream(() => reader).Sequence();

        Assert.True(result.IsSuccess);
        Assert.True(reader.Disposed);
    }

    [Fact]
    public void Reader_IsDisposed_WhenConsumerStopsEarly()
    {
        var reader = new TrackingReader(Samples.Csv);
        var first = CsvItemSource.Stream(() => reader).Take(2).ToList();

        Assert.Equal(2, first.Count);
        Assert.True(reader.Disposed);
    }

    [Fact]
    public void Reader_IsDisposed_WhenReadingThrows_AndTheExceptionPropagatesFromTheIterator()
    {
        var reader = new TrackingReader(Samples.Csv, failOnReadNumber: 3);

        Assert.Throws<IOException>(() => CsvItemSource.Stream(() => reader).ToList());
        Assert.True(reader.Disposed);
    }

    [Fact]
    public void Reader_IsDisposed_WhenARowIsMalformed_BecauseSequenceStopsEarly()
    {
        var reader = new TrackingReader("id,name,category,quantity,unit_price_cents,reorder_level\nA,n,c,x,1,1\nB,n,c,1,1,1\n");
        var result = CsvItemSource.Stream(() => reader).Sequence();

        Assert.False(result.IsSuccess);
        Assert.True(reader.Disposed);
    }

    [Fact]
    public void LoadSnapshot_MissingFile_IsAFileNotFoundFailure()
    {
        using var dir = new TempDir();
        var r = CsvItemSource.LoadSnapshot(dir.File("nope.csv"));

        Assert.False(r.IsSuccess);
        Assert.Equal(ErrorCode.FileNotFound, r.Error.Code);
    }

    [Fact]
    public void LoadSnapshot_ValidFile_ReturnsSnapshot_AndReleasesTheFileHandle()
    {
        using var dir = new TempDir();
        var path = dir.Write("ok.csv", Samples.Csv);

        var r = CsvItemSource.LoadSnapshot(path);

        Assert.True(r.IsSuccess);
        Assert.Equal(9, r.Value.Count);
        using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(exclusive.CanWrite);
    }

    [Fact]
    public void LoadSnapshot_MalformedContent_FailsSafely_AndStillReleasesTheFileHandle()
    {
        const string h = "id,name,category,quantity,unit_price_cents,reorder_level\n";
        using var dir = new TempDir();

        var cases = new (string Name, string Content, ErrorCode Code)[]
        {
            ("header.csv",  "wrong,header\nA,n,c,1,1,1\n",     ErrorCode.MalformedContent),
            ("empty.csv",   "",                                ErrorCode.MalformedContent),
            ("fields.csv",  h + "A,n,c,1,1\n",                 ErrorCode.MalformedContent),
            ("number.csv",  h + "A,n,c,many,1,1\n",            ErrorCode.MalformedContent),
            ("negative.csv",h + "A,n,c,-4,1,1\n",              ErrorCode.InvalidData),
        };

        foreach (var c in cases)
        {
            var path = dir.Write(c.Name, c.Content);
            var r = CsvItemSource.LoadSnapshot(path);

            Assert.False(r.IsSuccess);
            Assert.Equal(c.Code, r.Error.Code);
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
    }

    [Fact]
    public void LoadSnapshot_HeaderOnly_IsAnEmptySnapshot_NotAnError()
    {
        using var dir = new TempDir();
        var r = CsvItemSource.LoadSnapshot(dir.Write("h.csv", "id,name,category,quantity,unit_price_cents,reorder_level\n"));

        Assert.True(r.IsSuccess);
        Assert.Empty(r.Value);
    }

    [Fact]
    public void ReportExporter_UnwritableTarget_IsAnExportFailure_AndLeavesNoTempFile()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Root, "missing-folder", "out.csv");

        var r = ReportExporter.WriteLines(path, new[] { "a" });

        Assert.False(r.IsSuccess);
        Assert.Equal(ErrorCode.ExportFailed, r.Error.Code);
        Assert.False(File.Exists(path + ".tmp"));
    }
}