using InventoryApp;

var tests = new List<(string Name, Action Run)>
{
    ("Shortage reports missing units and never becomes negative", () =>
    {
        Equal(7, Item(quantity: 3).Shortage());
        Equal(0, Item(quantity: 10).Shortage());
        Equal(0, Item(quantity: 20).Shortage());
    }),
    ("Bind transforms a successful result", () =>
        Equal(7, Result<Product>.Success(Item(quantity: 3)).Bind(p => Result<int>.Success(p.Shortage())).Value)),
    ("Bind preserves errors without invoking the next step", () =>
    {
        var calls = 0;
        var result = Result<int>.Failure("Original", "reason").Bind(x => { calls++; return Result<int>.Success(x); });
        Equal(false, result.IsSuccess); Equal("Original", result.Error!.Code); Equal(0, calls);
    }),
    ("Product rejects blank fields", () =>
    {
        Equal(false, Product.Create(" ", "Name", "Category", 0, 0, 0).IsSuccess);
        Equal(false, Product.Create("P", null, "Category", 0, 0, 0).IsSuccess);
        Equal(false, Product.Create("P", "Name", "", 0, 0, 0).IsSuccess);
    }),
    ("Product rejects negative and excessive values", () =>
    {
        Equal(false, Product.Create("P", "Name", "Category", -1, 0, 0).IsSuccess);
        Equal(false, Product.Create("P", "Name", "Category", 0, -1, 0).IsSuccess);
        Equal(false, Product.Create("P", "Name", "Category", 0, 0, -1).IsSuccess);
        Equal(false, Product.Create("P", "Name", "Category", 1_000_001, 0, 0).IsSuccess);
    }),
    ("Product accepts boundaries and normalizes text", () =>
    {
        var result = Product.Create(" P ", " Name ", " Category ", 0, 1_000_000, 1_000_000);
        Equal(true, result.IsSuccess); Equal("P", result.Value.Id); Equal("Name", result.Value.Name);
    })
};

var failures = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception ex) { failures++; Console.WriteLine($"FAIL {test.Name}: {ex.Message}"); }
}
Console.WriteLine($"{tests.Count - failures}/{tests.Count} tests passed");
return failures == 0 ? 0 : 1;

static Product Item(string id = "P001", string category = "Stationery", int quantity = 3, int price = 250) =>
    Product.Create(id, id, category, quantity, 10, price).Value;

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"Expected {expected}, got {actual}");
}
