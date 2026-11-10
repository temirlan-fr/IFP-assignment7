using System.Text.Json;
using System.Diagnostics;
using static InventoryApp.Tests.Check;

namespace InventoryApp.Tests;

internal static class WorkflowTests
{
    public static IEnumerable<(string, Action)> Cases()
    {
        yield return ("Complete normal workflow integrates both reports real native calls and JSON export", () =>
        {
            using var folder = new TemporaryFolder();
            var input = Path.Combine(folder.Path, "products.json");
            var output = Path.Combine(folder.Path, "reports.json");
            File.Copy("data/products.json", input);
            var original = File.ReadAllText(input);
            var result = Run(input, output);
            Equal(true, result.IsSuccess);
            Equal("P001,P002,P005,P003", string.Join(',', result.Value.LowStock.Select(row => row.Id)));
            Equal("Electronics,Pantry,Stationery", string.Join(',', result.Value.Categories.Select(row => row.Category)));
            Equal(13450L, result.Value.NativeRestockTotalCents);
            Equal(23850L, result.Value.Categories.Sum(row => row.ValueCents));
            Equal(14L, result.Value.LowStock[0].StandardScore);
            Equal(35L, result.Value.LowStock[0].UrgentScore);
            using var exported = JsonDocument.Parse(File.ReadAllText(output));
            Equal(13450L, exported.RootElement.GetProperty("NativeRestockTotalCents").GetInt64());
            Equal(4, exported.RootElement.GetProperty("LowStock").GetArrayLength());
            Equal(3, exported.RootElement.GetProperty("Categories").GetArrayLength());
            Equal(original, File.ReadAllText(input));
            Equal(0, Directory.GetFiles(folder.Path, "*.tmp").Length);
        });
        yield return ("Complete native failure workflow preserves input and previous export after partial processing", () =>
        {
            using var folder = new TemporaryFolder();
            var input = Path.Combine(folder.Path, "products.json");
            var output = Path.Combine(folder.Path, "reports.json");
            File.Copy("data/products.json", input);
            var original = File.ReadAllText(input);
            File.WriteAllText(output, "previous successful export");
            var calls = 0;
            var native = new NativeRestockCalculator((quantity, level, price) =>
            {
                calls++;
                if (calls == 2) throw new DllNotFoundException();
                return 1750;
            });
            var result = Run(input, output, native);
            Equal(false, result.IsSuccess); Equal("NativeUnavailable", result.Error!.Code);
            Equal(2, calls);
            Equal(original, File.ReadAllText(input));
            Equal("previous successful export", File.ReadAllText(output));
            Equal(0, Directory.GetFiles(folder.Path, "*.tmp").Length);
        });
        yield return ("Invalid input workflow never calls native code or creates an export", () =>
        {
            using var folder = new TemporaryFolder();
            var input = Path.Combine(folder.Path, "invalid.json");
            var output = Path.Combine(folder.Path, "reports.json");
            File.WriteAllText(input, "[{}]");
            var calls = 0;
            var native = new NativeRestockCalculator((q, r, p) => { calls++; return 0; });
            Equal("InvalidData", Run(input, output, native).Error!.Code);
            Equal(0, calls); Equal(false, File.Exists(output));
        });
        yield return ("Empty dataset workflow exports valid empty reports and zero total", () =>
        {
            using var folder = new TemporaryFolder();
            var input = Path.Combine(folder.Path, "empty.json");
            var output = Path.Combine(folder.Path, "reports.json");
            File.WriteAllText(input, "[]");
            var result = Run(input, output);
            Equal(true, result.IsSuccess); Equal(0, result.Value.LowStock.Length);
            Equal(0, result.Value.Categories.Length); Equal(0L, result.Value.NativeRestockTotalCents);
            Equal(true, File.Exists(output));
        });
        yield return ("Fresh isolated process reports an actual missing native DLL", () =>
        {
            using var folder = new TemporaryFolder();
            foreach (var file in new[] { "InventoryApp.dll", "InventoryApp.deps.json", "InventoryApp.runtimeconfig.json" })
                File.Copy(Path.Combine(AppContext.BaseDirectory, file), Path.Combine(folder.Path, file));
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = folder.Path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            start.ArgumentList.Add(Path.Combine(folder.Path, "InventoryApp.dll"));
            start.ArgumentList.Add("--probe-native");
            using var process = Process.Start(start) ?? throw new Exception("Could not start native failure probe.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                throw new Exception("Native failure probe timed out.");
            }
            var text = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
            Equal(1, process.ExitCode);
            Equal(true, text.Contains("NativeUnavailable", StringComparison.Ordinal));
        });
    }

    private static Result<ReportBundle> Run(string input, string output, NativeRestockCalculator? native = null) =>
        Workflow.Run(input, output, Scoring.CreateScore(ScoreConfiguration.Create(2, 5).Value),
            Scoring.CreateScore(ScoreConfiguration.Create(5, 20).Value), native);
}
