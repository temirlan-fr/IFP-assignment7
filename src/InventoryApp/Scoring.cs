namespace InventoryApp;

public sealed record ScoreConfiguration
{
    public int ShortageWeight { get; }
    public int EmptyStockBonus { get; }
    private ScoreConfiguration(int weight, int bonus) { ShortageWeight = weight; EmptyStockBonus = bonus; }
    public static Result<ScoreConfiguration> Create(int weight, int bonus) =>
        weight is < 1 or > 100 || bonus is < 0 or > 1000
            ? Result<ScoreConfiguration>.Failure("InvalidConfiguration", "Weight must be 1 to 100 and bonus 0 to 1000.")
            : Result<ScoreConfiguration>.Success(new ScoreConfiguration(weight, bonus));
}

public static class Scoring
{
    public static Func<Product, long> CreateScore(ScoreConfiguration configuration) => product =>
        (long)product.Shortage() * configuration.ShortageWeight
        + (product.Shortage() > 0 && product.Quantity == 0 ? configuration.EmptyStockBonus : 0);
}
