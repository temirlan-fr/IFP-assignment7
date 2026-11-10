namespace InventoryApp;

public sealed record LowStockRow(string Id, string Name, string Category, int Quantity, int Shortage, long RestockCostCents);
public sealed record CategoryRow(string Category, int ProductCount, long Units, long ValueCents);

public static class Reports
{
    public static LowStockRow[] LowStock(IEnumerable<Product> products) => products
        .Where(p => p.Shortage() > 0)
        .Select(p => new LowStockRow(p.Id, p.Name, p.Category, p.Quantity, p.Shortage(), (long)p.Shortage() * p.UnitPriceCents))
        .OrderByDescending(row => row.Shortage)
        .ThenBy(row => row.Id, StringComparer.Ordinal)
        .ToArray();

    public static CategoryRow[] CategoryValues(IEnumerable<Product> products) => products
        .Where(p => p.Quantity > 0)
        .GroupBy(p => p.Category, StringComparer.Ordinal)
        .Select(group => new CategoryRow(group.Key, group.Count(), group.Sum(p => (long)p.Quantity), group.Sum(p => (long)p.Quantity * p.UnitPriceCents)))
        .OrderByDescending(row => row.ValueCents)
        .ThenBy(row => row.Category, StringComparer.Ordinal)
        .ToArray();
}
