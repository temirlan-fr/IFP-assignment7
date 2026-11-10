using InventoryApp;

var input = args.Length > 0 ? args[0] : "data/products.json";
var result = InventoryStore.Load(input).Bind(products =>
    Result<(LowStockRow[] LowStock, CategoryRow[] Categories)>.Success((Reports.LowStock(products), Reports.CategoryValues(products))));
if (!result.IsSuccess)
{
    Console.Error.WriteLine($"{result.Error!.Code}: {result.Error.Message}");
    return 1;
}
Console.WriteLine("Low stock report");
foreach (var row in result.Value.LowStock)
    Console.WriteLine($"{row.Id} {row.Name}: stock {row.Quantity}, shortage {row.Shortage}, restock cents {row.RestockCostCents}");
Console.WriteLine("Category value report");
foreach (var row in result.Value.Categories)
    Console.WriteLine($"{row.Category}: products {row.ProductCount}, units {row.Units}, value cents {row.ValueCents}");
return 0;
