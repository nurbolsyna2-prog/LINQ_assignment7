using InventoryApp.Domain;
using InventoryApp.Extensions;
using InventoryApp.Io;

namespace InventoryApp.Tests;

internal static class Samples
{
    public const string Csv =
        "id,name,category,quantity,unit_price_cents,reorder_level\n" +
        "A100,USB-C Cable,Accessories,5,1250,20\n" +
        "A101,Laptop Stand,Accessories,3,4500,10\n" +
        "A102,Mouse Pad,Accessories,40,800,15\n" +
        "E200,Monitor 27in,Electronics,2,24900,5\n" +
        "E201,Webcam HD,Electronics,12,5900,10\n" +
        "E202,Wireless Mouse,Electronics,4,2500,12\n" +
        "S300,Notebook A5,Stationery,100,350,50\n" +
        "S301,Gel Pen Blue,Stationery,8,150,40\n" +
        "S302,Stapler,Stationery,15,990,10\n";

    public static IReadOnlyList<Item> Items() =>
        CsvItemSource.Stream(() => new StringReader(Csv)).Sequence().Value;

    public static Item I(string id, int qty, int price, int reorder, string category = "Cat") =>
        Item.Create(id, "Name " + id, category, qty, price, reorder).Value;
}

/// <summary>StringReader that records disposal and can simulate an I/O failure.</summary>
internal sealed class TrackingReader : StringReader
{
    private readonly int _failOnReadNumber;
    private int _reads;

    public TrackingReader(string text, int failOnReadNumber = -1) : base(text) => _failOnReadNumber = failOnReadNumber;

    public bool Disposed { get; private set; }

    public override string? ReadLine()
    {
        _reads++;
        if (_reads == _failOnReadNumber) throw new IOException("simulated read failure");
        return base.ReadLine();
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>Temporary directory deleted after the test.</summary>
internal sealed class TempDir : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "inventory-tests-" + Guid.NewGuid().ToString("N"));

    public TempDir() => Directory.CreateDirectory(Root);

    public string File(string name) => Path.Combine(Root, name);

    public string Write(string name, string content)
    {
        var path = File(name);
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}