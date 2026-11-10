using System.Text.Json;

namespace InventoryApp;

public static class InventoryStore
{
    public static Result<Product[]> Load(string path)
    {
        try { return Parse(File.ReadAllText(path)); }
        catch (FileNotFoundException) { return Result<Product[]>.Failure("FileMissing", "Inventory file was not found."); }
        catch (DirectoryNotFoundException) { return Result<Product[]>.Failure("FileMissing", "Inventory directory was not found."); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { return Result<Product[]>.Failure("FileRead", ex.Message); }
    }

    public static Result<Product[]> Parse(string json)
    {
        try
        {
            var rows = JsonSerializer.Deserialize<ProductData?[]>(json, new JsonSerializerOptions
            { PropertyNameCaseInsensitive = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow });
            if (rows is null) return Result<Product[]>.Failure("InvalidData", "Expected a JSON array.");
            var products = new List<Product>(rows.Length);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (row is null || row.Quantity is null || row.ReorderLevel is null || row.UnitPriceCents is null)
                    return Result<Product[]>.Failure("InvalidData", "Every product must contain all required fields.");
                var product = Product.Create(row.Id, row.Name, row.Category, row.Quantity.Value, row.ReorderLevel.Value, row.UnitPriceCents.Value);
                if (!product.IsSuccess) return Result<Product[]>.Failure(product.Error!.Code, product.Error.Message);
                if (!ids.Add(product.Value.Id)) return Result<Product[]>.Failure("DuplicateId", "Product ids must be unique.");
                products.Add(product.Value);
            }
            return Result<Product[]>.Success(products.ToArray());
        }
        catch (JsonException ex) { return Result<Product[]>.Failure("MalformedJson", ex.Message); }
    }

    private sealed record ProductData(string? Id, string? Name, string? Category, int? Quantity, int? ReorderLevel, int? UnitPriceCents);
}
