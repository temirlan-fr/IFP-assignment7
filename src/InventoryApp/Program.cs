using InventoryApp;

if (args.Length == 1 && args[0] == "--probe-native")
{
    var probe = new NativeRestockCalculator().Calculate(3, 10, 250);
    Console.WriteLine(probe.IsSuccess ? $"Native restock cents: {probe.Value}" : $"{probe.Error!.Code}: {probe.Error.Message}");
    return probe.IsSuccess ? 0 : 1;
}
var simulateFailure = args.Contains("--simulate-native-failure", StringComparer.Ordinal);
var paths = args.Where(arg => arg != "--simulate-native-failure").ToArray();
var input = paths.Length > 0 ? paths[0] : "data/products.json";
var standardScore = Scoring.CreateScore(ScoreConfiguration.Create(2, 5).Value);
var urgentScore = Scoring.CreateScore(ScoreConfiguration.Create(5, 20).Value);
var output = paths.Length > 1 ? paths[1] : "artifacts/reports.json";
var native = simulateFailure
    ? new NativeRestockCalculator((quantity, level, price) => throw new DllNotFoundException())
    : new NativeRestockCalculator();
var result = Workflow.Run(input, output, standardScore, urgentScore, native);
if (!result.IsSuccess)
{
    Console.Error.WriteLine($"{result.Error!.Code}: {result.Error.Message}");
    return 1;
}
Console.WriteLine("Low stock report");
foreach (var row in result.Value.LowStock)
    Console.WriteLine($"{row.Id} {row.Name}: stock {row.Quantity}, shortage {row.Shortage}, restock cents {row.RestockCostCents}, standard {row.StandardScore}, urgent {row.UrgentScore}");
Console.WriteLine("Category value report");
foreach (var row in result.Value.Categories)
    Console.WriteLine($"{row.Category}: products {row.ProductCount}, units {row.Units}, value cents {row.ValueCents}");
Console.WriteLine($"Native total restock cost in cents: {result.Value.NativeRestockTotalCents}");
Console.WriteLine($"Reports exported to {Path.GetFullPath(output)}");
return 0;
