using InventoryApp.Domain;

namespace InventoryApp.Io;

public static class ReportExporter
{
    /// <summary>
    /// File.WriteAllLines is a CONVENIENCE API: it opens, writes and closes the file itself
    /// (owner = the API, released before it returns, also when it throws).
    /// Written to "path.tmp" first, then moved, so a failure never leaves a half-written export.
    /// </summary>
    public static Result<string> WriteLines(string path, IEnumerable<string> lines)
    {
        var tmp = path + ".tmp";
        try
        {
            File.WriteAllLines(tmp, lines);
            File.Move(tmp, path, overwrite: true);
            return Result.Ok(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* best-effort cleanup */ }
            return Result.Fail<string>(ErrorCode.ExportFailed, $"Cannot export to {path}: {ex.Message}");
        }
    }
}