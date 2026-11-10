namespace InventoryApp;

public sealed record Product
{
    public string Id { get; }
    public string Name { get; }
    public string Category { get; }
    public int Quantity { get; }
    public int ReorderLevel { get; }
    public int UnitPriceCents { get; }

    private Product(string id, string name, string category, int quantity, int reorderLevel, int unitPriceCents)
    {
        Id = id; Name = name; Category = category;
        Quantity = quantity; ReorderLevel = reorderLevel; UnitPriceCents = unitPriceCents;
    }

    public static Result<Product> Create(string? id, string? name, string? category,
        int quantity, int reorderLevel, int unitPriceCents)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
            return Result<Product>.Failure("InvalidProduct", "Id, name and category must not be blank.");
        if (quantity is < 0 or > 1_000_000 || reorderLevel is < 0 or > 1_000_000 || unitPriceCents is < 0 or > 1_000_000)
            return Result<Product>.Failure("InvalidProduct", "Quantity, reorder level and price must be between 0 and 1000000.");
        return Result<Product>.Success(new Product(id.Trim(), name.Trim(), category.Trim(), quantity, reorderLevel, unitPriceCents));
    }
}
