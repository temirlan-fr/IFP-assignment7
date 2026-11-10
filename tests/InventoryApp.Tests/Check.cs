namespace InventoryApp.Tests;

public static class Check
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected {expected}, got {actual}");
    }
    public static Product MakeProduct(string id, string category, int quantity, int level = 10, int price = 100) =>
        InventoryApp.Product.Create(id, id, category, quantity, level, price).Value;
}
