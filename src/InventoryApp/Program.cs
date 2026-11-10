using InventoryApp;

var shortage = Product.Create("P001", "Notebook", "Stationery", 3, 10, 250)
    .Bind(product => Result<int>.Success(product.Shortage()));
Console.WriteLine(shortage.IsSuccess ? $"Notebook shortage: {shortage.Value}" : shortage.Error!.Message);
return shortage.IsSuccess ? 0 : 1;
