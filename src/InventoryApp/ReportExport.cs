using System.Text.Json;

namespace InventoryApp;

public sealed record ReportBundle(LowStockRow[] LowStock, CategoryRow[] Categories, long NativeRestockTotalCents = 0);

public static class ReportExport
{
    public static Result<string> Save(string path, ReportBundle reports)
    {
        string? temporary = null;
        Result<string> result;
        Error? cleanupError = null;
        try
        {
            var destination = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(directory);
            temporary = Path.Combine(directory, $".inventory-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(temporary, JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, destination, overwrite: true);
            result = Result<string>.Success(destination);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { result = Result<string>.Failure("FileWrite", ex.Message); }
        finally
        {
            try
            {
                if (temporary is not null && File.Exists(temporary))
                    File.Delete(temporary);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { cleanupError = new Error("FileCleanup", $"Temporary export could not be removed: {ex.Message}"); }
        }
        return cleanupError is null ? result : Result<string>.Failure(cleanupError.Code, cleanupError.Message);
    }
}
