using InventoryApp;

var input = args.Length > 0 ? args[0] : "data/products.json";
var standardScore = Scoring.CreateScore(ScoreConfiguration.Create(2, 5).Value);
var urgentScore = Scoring.CreateScore(ScoreConfiguration.Create(5, 20).Value);
var output = args.Length > 1 ? args[1] : "artifacts/reports.json";
var result = Workflow.Run(input, output, standardScore, urgentScore);
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
Console.WriteLine($"Reports exported to {Path.GetFullPath(output)}");
return 0;
