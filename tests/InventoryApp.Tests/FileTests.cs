using static InventoryApp.Tests.Check;

namespace InventoryApp.Tests;

internal static class FileTests
{
    public static IEnumerable<(string, Action)> Cases()
    {
        yield return ("Missing file returns a safe failure", () =>
        {
            using var folder = new TemporaryFolder();
            Equal("FileMissing", InventoryStore.Load(Path.Combine(folder.Path, "missing.json")).Error!.Code);
            Equal(0, Directory.GetFiles(folder.Path).Length);
        });
        yield return ("Malformed file returns failure and releases its handle", () =>
        {
            using var folder = new TemporaryFolder();
            var path = Path.Combine(folder.Path, "broken.json");
            File.WriteAllText(path, "{broken");
            Equal("MalformedJson", InventoryStore.Load(path).Error!.Code);
            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Equal(true, stream.CanWrite);
            File.Delete(path); Equal(false, File.Exists(path));
        });
        yield return ("Successful load releases the file before deferred processing", () =>
        {
            using var folder = new TemporaryFolder();
            var path = Path.Combine(folder.Path, "products.json");
            File.Copy("data/products.json", path);
            var data = InventoryStore.Load(path).Value;
            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Equal(true, stream.CanWrite);
            File.Delete(path);
            Equal(4, Reports.LowStock(data).Length);
        });
        yield return ("Export releases handles on success and cleans a failed replacement", () =>
        {
            using var folder = new TemporaryFolder();
            var reports = new ReportBundle([], []);
            var destination = Path.Combine(folder.Path, "report.json");
            Equal(true, ReportExport.Save(destination, reports).IsSuccess);
            using (var stream = File.Open(destination, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Equal(true, stream.Length > 0);
            var blocked = Path.Combine(folder.Path, "existing-directory");
            Directory.CreateDirectory(blocked);
            Equal("FileWrite", ReportExport.Save(blocked, reports).Error!.Code);
            Equal(0, Directory.GetFiles(folder.Path, "*.tmp").Length);
            Equal(true, Directory.Exists(blocked));
        });
        yield return ("Workflow refuses to overwrite its input", () =>
        {
            using var folder = new TemporaryFolder();
            var path = Path.Combine(folder.Path, "products.json");
            File.Copy("data/products.json", path);
            var before = File.ReadAllText(path);
            Equal("UnsafeOutput", Workflow.Run(path, path, p => p.Shortage(), p => p.Shortage()).Error!.Code);
            Equal(before, File.ReadAllText(path));
        });
    }
}

internal sealed class TemporaryFolder : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InventoryTests-" + Guid.NewGuid().ToString("N"));
    public TemporaryFolder() => Directory.CreateDirectory(Path);
    public void Dispose() => Directory.Delete(Path, recursive: true);
}
