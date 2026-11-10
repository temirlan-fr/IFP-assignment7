namespace InventoryApp;

public static class Workflow
{
    public static Result<ReportBundle> Run(string inputPath, string outputPath,
        Func<Product, long> standardScore, Func<Product, long> urgentScore, NativeRestockCalculator? native = null)
    {
        try
        {
            if (string.Equals(Path.GetFullPath(inputPath), Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
                return Result<ReportBundle>.Failure("UnsafeOutput", "Output must not replace the input inventory.");
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
        { return Result<ReportBundle>.Failure("InvalidPath", ex.Message); }

        return InventoryStore.Load(inputPath).Bind(products =>
        {
            var calculator = native ?? new NativeRestockCalculator();
            long nativeTotal = 0;
            foreach (var product in products)
            {
                var cost = calculator.Calculate(product.Quantity, product.ReorderLevel, product.UnitPriceCents);
                if (!cost.IsSuccess) return Result<ReportBundle>.Failure(cost.Error!.Code, cost.Error.Message);
                nativeTotal = checked(nativeTotal + cost.Value);
            }
            var reports = new ReportBundle(Reports.LowStock(products, standardScore, urgentScore), Reports.CategoryValues(products), nativeTotal);
            return ReportExport.Save(outputPath, reports).Bind(_ => Result<ReportBundle>.Success(reports));
        });
    }
}
