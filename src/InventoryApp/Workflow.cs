namespace InventoryApp;

public static class Workflow
{
    public static Result<ReportBundle> Run(string inputPath, string outputPath,
        Func<Product, long> standardScore, Func<Product, long> urgentScore)
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
            var reports = new ReportBundle(Reports.LowStock(products, standardScore, urgentScore), Reports.CategoryValues(products));
            return ReportExport.Save(outputPath, reports).Bind(_ => Result<ReportBundle>.Success(reports));
        });
    }
}
