using System.Globalization;
using InventoryApp.Domain;
using InventoryApp.Extensions;

namespace InventoryApp.Io;

public static class CsvItemSource
{
    public const string Header = "id,name,category,quantity,unit_price_cents,reorder_level";

    /// <summary>
    /// LAZY stream (yield return). Calling this method runs NOTHING: the reader is not opened.
    /// OWNER of the TextReader = this iterator. RELEASE = the 'using' below, which runs when
    /// iteration finishes, when the consumer stops early or when reading throws.
    /// </summary>
    public static IEnumerable<Result<Item>> Stream(Func<TextReader> open)
    {
        using var reader = open();

        var header = reader.ReadLine();
        if (header is null || header.Trim() != Header)
        {
            yield return Result.Fail<Item>(ErrorCode.MalformedContent, $"Line 1: expected header '{Header}'.");
            yield break;
        }

        int lineNo = 1;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            yield return ParseLine(line, lineNo);
        }
    }

    /// <summary>Bounded resource scope: materialises everything, disposes the reader, returns a snapshot.</summary>
    public static Result<IReadOnlyList<Item>> LoadSnapshot(string path)
    {
        if (!File.Exists(path))
            return Result.Fail<IReadOnlyList<Item>>(ErrorCode.FileNotFound, $"File not found: {path}");
        try
        {
            return Stream(() => new StreamReader(path)).Sequence();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Fail<IReadOnlyList<Item>>(ErrorCode.FileUnreadable, $"Cannot read {path}: {ex.Message}");
        }
    }

    internal static Result<Item> ParseLine(string line, int lineNo)
    {
        var f = line.Split(',');
        if (f.Length != 6)
            return Result.Fail<Item>(ErrorCode.MalformedContent, $"Line {lineNo}: expected 6 fields, found {f.Length}.");

        if (!int.TryParse(f[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var qty))
            return Result.Fail<Item>(ErrorCode.MalformedContent, $"Line {lineNo}: quantity '{f[3]}' is not an integer.");
        if (!int.TryParse(f[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var price))
            return Result.Fail<Item>(ErrorCode.MalformedContent, $"Line {lineNo}: unit_price_cents '{f[4]}' is not an integer.");
        if (!int.TryParse(f[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var reorder))
            return Result.Fail<Item>(ErrorCode.MalformedContent, $"Line {lineNo}: reorder_level '{f[5]}' is not an integer.");

        return Item.Create(f[0], f[1], f[2], qty, price, reorder);
    }
}