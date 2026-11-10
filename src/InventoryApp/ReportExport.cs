using System.Text.Json;

namespace InventoryApp;

public sealed record ReportBundle(LowStockRow[] LowStock, CategoryRow[] Categories);

public static class ReportExport
{
    public static Result<string> Save(string path, ReportBundle reports)
    {
        string? temporary = null;
        try
        {
            var destination = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(directory);
            temporary = Path.Combine(directory, $".inventory-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(temporary, JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, destination, overwrite: true);
            return Result<string>.Success(destination);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { return Result<string>.Failure("FileWrite", ex.Message); }
        finally
        {
            if (temporary is not null && File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
