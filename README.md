Inventory reports with extension methods and LINQ

Author

Temirlan Bolat
Group IT-2504
Individual project

Purpose

This reusable inventory module supports purchasing and stock valuation. Products are immutable and validation returns explicit Result failures. Prices use integer cents. Quantities, reorder levels and unit prices range from zero to one million.

Task 1 Custom extension methods

InventoryExtensions.Shortage receives a Product and returns an integer number of units needed to reach its reorder level. It belongs on Product because shortage is derived from that product alone. A stocked product returns zero. The method never changes the product.

InventoryExtensions.Bind receives a Result and a function returning another Result. It returns the next result on success and preserves the original error on failure without calling the next step. It belongs on Result because it composes validated pipeline stages without exposing a failed value.

Both methods are called in Program.cs and covered by assertions in the test runner.

Prerequisites and commands

Run commands from the repository root. Install the .NET SDK 8.0.425. The project uses .NET 8 and no external packages.

dotnet build src/InventoryApp/InventoryApp.csproj

dotnet run --project src/InventoryApp

dotnet run --project tests/InventoryApp.Tests

The assertion based test runner returns exit code one on failure.

Task 5 Resource ownership and export

InventoryStore.Load uses File.ReadAllText. This convenience API owns and closes its internal reader and file handle on success and failure. Validation and deferred queries run after the file is closed. They retain only managed product data and never yield from an open file.

ReportExport.Save uses File.WriteAllText, which owns and closes its internal writer. It writes a uniquely named temporary file beside the destination, then moves the completed file into place. A finally block removes an unfinished temporary file. Failed replacement leaves an existing destination untouched. Workflow.Run rejects an output path equal to the input path and returns explicit errors before displaying success. Product data is never modified.

FileTests uses using scopes for temporary folders and exclusive FileStream probes. Tests assert that successful and malformed reads permit immediate exclusive access and deletion. Export tests check exclusive access after success and removal of temporary files after a failed replacement. Missing files are safe FileMissing results. The default export is artifacts/reports.json. Latest result after Task 5 is 20 tests passed.

Task 4 Deferred execution

Reports.ProjectStock uses yield return to project actual products into stock report rows. Creating the enumerable and obtaining its enumerator do not process a product. MoveNext processes the next product. DeferredTests uses a test only scoring probe to assert zero calls before iteration, one call after the first item, and the expected total after enumeration. Production calculations contain no diagnostic counters.

Reports.LowStock composes ProjectStock, Where, OrderByDescending and ThenBy, then deliberately calls ToArray. Reports.CategoryValues also materialises its final output. The application can display and export those stable rows without recalculating them. Enumerating ProjectStock again before materialisation recomputes pure projections over the in memory product snapshot. Enumerating a materialised array again only reads saved rows. Neither operation rereads a file or changes business state. Latest result after Task 4 is 15 tests passed.

Task 3 Currying and partial application

Scoring.CreateScore takes an immutable validated ScoreConfiguration and returns a function from Product to long. Arguments arrive in successive calls. Program.cs fixes weight 2 and empty stock bonus 5 for standard purchasing, then weight 5 and bonus 20 for urgent purchasing. Both configured functions are passed into Reports.LowStock. No global setting changes.

Score is shortage times weight plus an empty stock bonus only when a shortage exists. Notebook receives 14 and 35 on the same input. Tea receives 13 and 40. A product with no shortage scores zero. Tests assert both configured functions and invalid configuration boundaries. Latest result after Task 3 is 14 tests passed.

Task 2 LINQ reports

Input is data/products.json containing six inventory products. InventoryStore.Load reads JSON and InventoryStore.Parse validates every row before returning a complete snapshot. Missing numeric fields, null rows, duplicate ids, negative values and invalid JSON produce explicit failures.

Reports.LowStock helps a purchasing officer identify products below their reorder level. It uses Where to filter shortage above zero, Select to project id, name, category, quantity, shortage and restock cost in cents, then OrderByDescending on shortage and ThenBy on ordinal id. Expected order is P001 P002 P005 P003. Shortages are 7 7 4 3 and restock costs are 1750 700 2000 9000 cents.

Reports.CategoryValues helps a warehouse manager assess available inventory. It filters positive quantities, groups by ordinal category, counts product types, sums units and sums quantity times price in 64 bit integer cents. It sorts value descending and category ascending to break ties. Expected results are Electronics with 2 products 10 units and 18000 cents, Pantry with 1 product 6 units and 4800 cents, and Stationery with 2 products 6 units and 1050 cents. Zero stock contributes no available value.

Both reports are pure transformations in Reports.cs. Program.cs alone displays rows. Tests cover normal values, empty data, no matches, reversed input with tied values, grouping, invalid records and 64 bit monetary arithmetic. Latest result after Task 2 is 12 tests passed.
