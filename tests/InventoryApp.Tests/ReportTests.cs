using InventoryApp;
using static InventoryApp.Tests.Check;

namespace InventoryApp.Tests;

internal static class ReportTests
{
    public static IEnumerable<(string, Action)> Cases()
    {
        yield return ("Reports return correct projected values and grouped totals", () =>
        {
            var data = new[] { MakeProduct("B", "Office", 3), MakeProduct("A", "Office", 2, price: 250), MakeProduct("C", "Food", 0) };
            var low = Reports.LowStock(data);
            Equal("C,A,B", string.Join(',', low.Select(r => r.Id)));
            Equal(2000L, low[1].RestockCostCents);
            var categories = Reports.CategoryValues(data);
            Equal(1, categories.Length); Equal("Office", categories[0].Category);
            Equal(2, categories[0].ProductCount); Equal(5L, categories[0].Units); Equal(800L, categories[0].ValueCents);
        });
        yield return ("Reports handle an empty dataset", () =>
        { Equal(0, Reports.LowStock([]).Length); Equal(0, Reports.CategoryValues([]).Length); });
        yield return ("Reports handle no matching records", () =>
        {
            Equal(0, Reports.LowStock([MakeProduct("A", "Office", 10)]).Length);
            Equal(0, Reports.CategoryValues([MakeProduct("A", "Office", 0)]).Length);
        });
        yield return ("Reports break ties deterministically regardless of input order", () =>
        {
            var data = new[] { MakeProduct("B", "Z", 3), MakeProduct("A", "A", 3) };
            Equal("A,B", string.Join(',', Reports.LowStock(data).Select(r => r.Id)));
            Equal("A,B", string.Join(',', Reports.LowStock(data.Reverse()).Select(r => r.Id)));
            Equal("A,Z", string.Join(',', Reports.CategoryValues(data).Select(r => r.Category)));
            Equal("A,Z", string.Join(',', Reports.CategoryValues(data.Reverse()).Select(r => r.Category)));
        });
        yield return ("JSON validation rejects missing values duplicate ids and invalid domain data", () =>
        {
            Equal(false, InventoryStore.Parse("[{}]").IsSuccess);
            Equal(false, InventoryStore.Parse("[null]").IsSuccess);
            Equal(false, InventoryStore.Parse("null").IsSuccess);
            const string row = """{"id":"A","name":"A","category":"Office","quantity":1,"reorderLevel":2,"unitPriceCents":100}""";
            Equal("DuplicateId", InventoryStore.Parse("[" + row + "," + row + "]").Error!.Code);
            Equal("InvalidProduct", InventoryStore.Parse("[" + row.Replace("\"quantity\":1", "\"quantity\":-1") + "]").Error!.Code);
            Equal(0, InventoryStore.Parse("[]").Value.Length);
        });
        yield return ("Report money calculations do not overflow 32 bit integers", () =>
        {
            var data = new[] { MakeProduct("A", "Office", 1_000_000, 1_000_000, 1_000_000) };
            Equal(1_000_000_000_000L, Reports.CategoryValues(data)[0].ValueCents);
        });
    }
}
