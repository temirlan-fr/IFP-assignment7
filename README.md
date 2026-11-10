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

Task 8 C sharp native binding and workflow

NativeRestockCalculator.NativeMethods.RestockCost uses DllImport with the exact export inventory_restock_cost and CallingConvention.Cdecl. C int32_t maps to C sharp int and C int64_t maps to C sharp long. Directory.Build.props sets PlatformTarget to x64. No unsafe code or extra package is required.

Directory.Build.targets requires the native build and copies inventory_native.dll beside both the application and test assemblies on build and publish. The binding searches the assembly directory. A clean build therefore runs the native script first. The DLL is owned by the runtime for the process lifetime and the C function creates no resources requiring manual release.

NativeRestockCalculator.Calculate validates input before invoking C and returns explicit NativeUnavailable, NativeEntryPoint, NativeArchitecture or NativeRejected failures at the boundary. Tests inject controlled exceptions to verify failure translation. Normal operation always uses the real P Invoke binding and contains no managed fallback.

Workflow.Run loads and validates the dataset, calls the native adapter for every product, then passes the configured scoring functions into the pure reports and exports their complete snapshot. The native total is displayed and exported. The sample total is 13450 cents. The failure path returns before writing an export or changing input. Native loading and file operations remain outside Reports.cs and Scoring.cs.

dotnet run --project src/InventoryApp -- --probe-native

This invokes the real DLL and prints 1750 cents. The integration tests also assert a real 1750 cent result, a stocked product result of zero and the maximum supported amount. Latest result after Task 8 is 23 tests passed.

Task 7 Native C library contract

native/inventory_native.c and native/inventory_native.h implement inventory_restock_cost. Its three arguments are int32_t quantity, reorder_level and unit_price_cents. Each accepts zero through one million inclusive. The return type is int64_t and represents the missing units times the unit price in cents. A product at or above its reorder level returns zero. Invalid input returns minus one. The export uses the C calling convention cdecl and fixed width types to avoid platform dependent long sizes.

All money is expressed in integer cents and multiplication is exact, so no rounding is needed. The maximum valid result is one trillion cents and fits in 64 bits. Manually checked example for Notebook is quantity 3, reorder level 10 and unit price 250. Seven missing units times 250 cents gives 1750 cents. The function performs no allocation and returns no pointers or handles.

The supported native target is Windows x64. scripts/setup-compiler.ps1 downloads portable Zig 0.14.1 from the official Zig site and verifies its published SHA256. It does not install a system toolchain. scripts/build-native.ps1 invokes Zig cc with C11, warnings as errors, optimisation and the x86_64 Windows GNU target. Source is compiled into native/build/win-x64/inventory_native.dll. Generated binaries and the compiler directory are ignored by Git.

Run powershell -NoProfile -ExecutionPolicy Bypass -File scripts/setup-compiler.ps1 once, then powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-native.ps1. The build also compiles and runs native/native_tests.c with assertions for the hand calculated example, stocked products, invalid arguments and the largest valid result. These C assertions supplement the managed test suite.

Task 6 Retention and allocation review

InventoryStore.Load first materialises the JSON text. Parse temporarily creates deserialised rows, a validation list and a set of ids, then returns one Product array. These temporary parsing objects become eligible for collection after Load returns. The dataset is read once per workflow invocation and shared by both reports.

ProjectStock retains a reference to its source and two scoring delegates until its enumerable can be collected. Each scoring closure retains only its small immutable ScoreConfiguration and does not capture the product collection. Materialising the final reports with ToArray avoids repeating sorting, grouping and row allocations when displaying and exporting the same results. LINQ ordering and grouping also allocate temporary buffers during enumeration.

The program has no long lived collection, growing cache, event subscription or background service. Local arrays and report rows remain reachable for the duration of one command and are then eligible for garbage collection. There is no claim that a memory leak was fixed and no manual garbage collection.

Streaming could reduce peak memory for very large input, but sorting and grouping still need working storage and an open file iterator requires careful disposal. This project deliberately loads a bounded file snapshot so errors are resolved before reporting and repeated queries do not reread files or use disposed resources. The trade off is memory proportional to the input size. No fixed performance target is claimed.

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
