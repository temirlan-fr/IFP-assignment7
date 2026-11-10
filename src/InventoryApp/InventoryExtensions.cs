namespace InventoryApp;

public static class InventoryExtensions
{
    public static int Shortage(this Product product) => Math.Max(0, product.ReorderLevel - product.Quantity);
    public static Result<TNext> Bind<T, TNext>(this Result<T> result, Func<T, Result<TNext>> next) =>
        result.IsSuccess ? next(result.Value) : Result<TNext>.Failure(result.Error!.Code, result.Error.Message);
}
