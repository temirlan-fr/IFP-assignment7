Inventory reports with extension methods and LINQ

Author and contribution

Temirlan Bolat
Group IT-2504
Individual project

Contribution scope covers the inventory domain, Result pipeline, extension methods, LINQ reports, configured scoring functions, deferred projection, file boundaries, native C library, C sharp adapter, automated tests and documentation. This module can be retained and integrated into the final project.

Project purpose and input

The application helps a purchasing officer plan replenishment and a warehouse manager review stock value. It reads data/products.json, runs two reports, calculates total replenishment cost through a real native C library, displays results and exports artifacts/reports.json.

Each product has an id, name, category, quantity, reorder level and unit price in cents. Id, name and category must be nonblank. Ids must be unique and use ordinal comparison. Quantities, reorder levels and prices must be integers from zero to one million inclusive. Missing numeric fields, null rows, unknown fields, duplicate ids, malformed JSON and invalid domain values produce explicit Result failures. Product objects are immutable. Money uses 64 bit integer cents and no fractional rounding.

Platform and prerequisites

The verified platform is Windows 11 x64. The project targets .NET 8 and pins SDK 8.0.425 in global.json. Native code uses Clang 19.1.7 included in portable Zig 0.14.1. The compiler target is x86_64 Windows GNU. No NuGet packages, Visual Studio installation or unsafe C sharp code are required.

Run all commands below in PowerShell from the extracted repository root. The root contains README.md, global.json, scripts, native, data, src and tests. Initial compiler setup requires internet access and Windows curl.exe. The setup script downloads the official Zig archive, verifies SHA256 and extracts it into .tools. Generated compiler files are not committed.

Build run and test

powershell -NoProfile -ExecutionPolicy Bypass -File scripts/setup-compiler.ps1

powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-native.ps1

dotnet build tests/InventoryApp.Tests/InventoryApp.Tests.csproj -c Release

dotnet run --no-build -c Release --project src/InventoryApp

dotnet run --no-build -c Release --project tests/InventoryApp.Tests

The test project builds the application through its project reference. Its assertion based runner compares expected and actual results, prints PASS or FAIL for each named case and exits with code one if any case fails. It is intentionally executed with dotnet run rather than dotnet test.

After compiler setup, this single command rebuilds native code, runs C assertions, builds C sharp, runs managed tests and launches the application.

powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1

An existing Zig installation can be used by passing its full executable path through the Compiler parameter of build-native.ps1 or verify.ps1.

Expected sample results

The low stock report contains P001 Notebook, P002 Pen, P005 Tea and P003 Keyboard in that order. Their shortages are 7, 7, 4 and 3 units. Their restock costs are 1750, 700, 2000 and 9000 cents.

Standard and urgent scores respectively are 14 and 35 for Notebook, 14 and 35 for Pen, 13 and 40 for Tea, and 6 and 15 for Keyboard.

The category report contains Electronics with 2 product types, 10 units and 18000 cents, Pantry with 1 product type, 6 units and 4800 cents, and Stationery with 2 product types, 6 units and 1050 cents. Total available inventory value is 23850 cents. Zero stock products do not contribute to this report.

The actual native replenishment total is 13450 cents. The exported JSON contains LowStock, Categories and NativeRestockTotalCents.

Task 1 Custom extension methods

src/InventoryApp/InventoryExtensions.cs defines Shortage and Bind.

Shortage receives Product and returns int. It returns the positive difference between reorder level and quantity or zero when no replenishment is needed. It belongs on Product because its result depends only on that product. Reports.ProjectStock and Scoring.CreateScore call it without changing shared state.

Bind receives Result and a function returning another Result. It returns the configured next stage on success and preserves the original error without invoking the next stage on failure. It belongs on Result because it composes validated pipeline stages. Workflow.Run uses it to connect loading, calculation and export.

Tests in tests/InventoryApp.Tests/Program.cs assert both extensions, error preservation and domain validation.

Task 2 LINQ reports

src/InventoryApp/Reports.cs contains both pure reports. File access and Console output are outside this class.

LowStock accepts validated products and two configured scoring functions. Its intended user is a purchasing officer. ProjectStock projects product fields into LowStockRow. Where keeps shortages above zero. OrderByDescending sorts shortage and ThenBy uses ordinal id to break ties. Output fields are id, name, category, quantity, shortage, restock cost, standard score and urgent score.

CategoryValues accepts the same validated product snapshot. Its intended user is a warehouse manager. Where keeps positive quantities. GroupBy groups by ordinal category. Select projects category, product count, unit total and inventory value. Count and Sum aggregate the values. OrderByDescending sorts inventory value and ThenBy uses ordinal category as a deterministic tie breaker.

Both methods return arrays. ReportTests asserts normal values, correct grouping, totals, empty input, no matches and reversed input with equal primary sort values. Monetary multiplication is widened to long before calculation.

Task 3 Currying and partial application

src/InventoryApp/Scoring.cs defines CreateScore. It receives ScoreConfiguration and returns a function from Product to long, so arguments arrive in successive calls. Program.cs partially applies weight 2 and empty stock bonus 5 for standard purchasing, then weight 5 and bonus 20 for urgent purchasing. Both functions are passed through Workflow.Run into the report.

Score is shortage times weight plus an empty stock bonus only when there is a shortage and quantity is zero. Notebook produces 14 and 35 on the same input. A product with no shortage scores zero. ScoringTests asserts both functions and rejects invalid configurations. Weight must be 1 to 100 and bonus 0 to 1000. No mutable global setting switches configuration.

Task 4 Deferred execution

Reports.ProjectStock implements a meaningful IEnumerable workflow with yield return. Creating the enumerable or obtaining its enumerator does not process products. MoveNext projects the next actual product.

DeferredTests injects a scoring probe only in test code. The counter remains zero before iteration, reaches one after the first item, reaches three after materialising the two item source following that early stop, and remains three while reading the saved array twice. Enumerating the deferred source again increases it to five. Production calculations contain no diagnostic counter.

LowStock and CategoryValues deliberately call ToArray at their final stage. Repeated enumeration before materialisation recomputes pure projections, sorting and grouping. Repeated use after materialisation reads stable saved rows. It does not reread files, consume a disposed stream or change business state.

Task 5 Resource ownership and safe transitions

src/InventoryApp/InventoryStore.cs uses File.ReadAllText. This convenience API owns and closes its internal reader and handle on success and failure. Parse validates the complete data before returning a Product array. All deferred processing happens after the file closes.

src/InventoryApp/ReportExport.cs uses File.WriteAllText, which owns and closes its internal writer. Save creates a unique temporary file beside the destination and moves the completed export into place. Its finally block removes an unfinished temporary file. FileWrite and FileCleanup describe controlled write and cleanup errors. A failed replacement does not truncate an existing destination.

src/InventoryApp/Workflow.cs rejects an output path equal to its input. A failed validation or native call returns before export. Products remain unchanged, and a failed native operation leaves an existing export untouched. No partial report is exposed as a successful result.

FileTests uses using for temporary folders and exclusive FileStream probes. Tests check immediate exclusive access and deletion after successful and malformed reads, successful export handle release, and temporary file cleanup after a failed replacement. No live file resource is yielded.

Task 6 Retention and allocation

Load materialises JSON text. Parse temporarily holds deserialised rows, a validation list and a set of ids before returning a Product array. Temporary parser objects become eligible for collection after loading. The same product snapshot feeds both reports in one workflow.

A ProjectStock enumerable retains its source and scoring delegates until it becomes unreachable. Scoring closures retain only small immutable configurations and do not capture a dataset. Materialised report arrays avoid repeated row allocation, sorting and grouping during display and export. Ordering and grouping also allocate temporary buffers while executing.

There are no long lived caches, growing collections, event subscriptions or background services. Local arrays live for one command. No memory leak fix, profiling result or manual garbage collection is claimed.

Streaming could reduce peak memory for a large input but requires careful file iterator disposal. Sorting and grouping still need working storage. This project chooses a snapshot so file handles close before querying, errors are resolved before report export and repeated queries do not reread files. The trade off is memory proportional to input size, including the full JSON text and parsed data.

Task 7 Native C contract

native/inventory_native.c and native/inventory_native.h implement the exported C function inventory_restock_cost with the cdecl calling convention.

Inputs are int32_t quantity, int32_t reorder_level and int32_t unit_price_cents. Each valid range is zero through one million inclusive. The return type is int64_t and the unit is cents. A stocked product returns zero. Otherwise the result is reorder level minus quantity multiplied by unit price. Invalid input returns minus one.

For quantity 3, reorder level 10 and unit price 250, the manually calculated result is 7 times 250, giving 1750 cents. The largest valid result is one trillion cents. Integer multiplication is exact and no rounding is applied. Fixed width integer types avoid platform dependent long assumptions. The function allocates no memory and returns no pointers or handles.

Task 8 Native build binding and workflow

scripts/build-native.ps1 compiles the shared DLL using Zig cc with C11, Wall, Wextra, Werror, O2, shared and target x86_64-windows-gnu options. The exact compiler invocation is in that script. It also builds and runs native/native_tests.c. The DLL location is native/build/win-x64/inventory_native.dll.

Directory.Build.props sets PlatformTarget to x64. Directory.Build.targets requires the DLL and copies it beside application and test assemblies during build and publish. In Release builds these locations are src/InventoryApp/bin/Release/net8.0 and tests/InventoryApp.Tests/bin/Release/net8.0. A clean build runs the native build first.

src/InventoryApp/NativeRestockCalculator.cs defines NativeMethods.RestockCost with DllImport, ExactSpelling, export name inventory_restock_cost and CallingConvention.Cdecl. C int32_t maps to C sharp int and int64_t maps to C sharp long. The loader searches the assembly directory. The runtime owns the loaded DLL for the process lifetime.

Calculate validates domain values before calling C. It converts missing DLL, missing export, wrong architecture and rejected return values into NativeUnavailable, NativeEntryPoint, NativeArchitecture and NativeRejected failures. Normal operation always uses the real native binding without a managed replacement.

Workflow.Run invokes the adapter for each loaded product and includes the returned total in the displayed and exported report bundle. File operations and native loading remain outside pure query transformations.

Task 9 Integration verification and reproducibility

The integration difficulty was that no C compiler was installed or available on PATH. A portable Zig 0.14.1 toolchain with a verified archive hash resolved that without requiring a machine wide installation. Explicit x64 targeting, fixed width types and copying the DLL beside each executable make the binding reproducible.

NativeTests calls the actual DLL and checks 1750 cents, zero for a stocked product and the maximum result. Injected loading, binding and architecture failures verify the adapter outcomes. WorkflowTests also launches a new process containing the managed application files without the native DLL. It asserts exit code one and NativeUnavailable. This is a real fresh load failure, independent of a DLL already loaded in the test process.

Latest verification on 10 November 2026 passed all 28 managed tests and all 7 native C assertions. The managed suite includes 15 pure calculation or validation scenarios before file and native tests, more than the required eight, and four complete workflow scenarios. Missing and malformed files each have their own test. A clean source archive extraction was rebuilt and run using the same compiler version and commands.

Automated test cases and expected results

Shortage returns missing units and never a negative value.
Bind transforms a success into the next result.
Bind preserves an error and does not call the next stage.
Blank product fields fail validation.
Negative and excessive product values fail validation.
Valid boundaries are accepted and text is trimmed.
Normal reports project correct costs and grouped totals.
Empty input returns empty report arrays.
No matching records returns empty reports.
Tied sort values retain deterministic order after input reversal.
Invalid JSON records and duplicate ids fail validation.
Large monetary calculations retain the correct 64 bit value.
Both configured functions produce the documented different scores.
Invalid scoring configurations fail validation.
Deferred processing runs only during iteration and snapshot reuse repeats no work.
A missing file returns FileMissing.
Malformed content returns MalformedJson and releases the file.
A successful read releases the file before querying its snapshot.
Export releases handles and removes temporary files after failure.
The workflow refuses to overwrite its input.
The real native library returns manually checked values.
Invalid native inputs are rejected before invoking C.
Loading and binding exceptions become explicit adapter failures.
The normal complete workflow exports both reports and a real native total.
A native failure after partial processing preserves input and the previous export.
Invalid workflow input never invokes native code or creates an export.
An empty dataset exports empty reports and a zero total.
A fresh process without the native DLL reports NativeUnavailable.

Normal and failure demonstrations

dotnet run --no-build -c Release --project src/InventoryApp -- data/products.json artifacts/custom-report.json

dotnet run --no-build -c Release --project src/InventoryApp -- --probe-native

The native probe calls the real DLL and prints 1750 cents.

dotnet run --no-build -c Release --project src/InventoryApp -- data/missing.json

This reports FileMissing and exits with code one.

dotnet run --no-build -c Release --project src/InventoryApp -- data/malformed.json

This reports MalformedJson and exits with code one.

dotnet run --no-build -c Release --project src/InventoryApp -- data/invalid-products.json

This reports InvalidProduct and exits with code one.

dotnet run --no-build -c Release --project src/InventoryApp -- --simulate-native-failure

This deliberately injects a missing library exception, reports NativeUnavailable and leaves an existing export unchanged. The automated isolated process test covers a real missing library as well.

Defense preparation

Run the normal workflow and trace the report operators in Reports.cs. Explain receiver types in InventoryExtensions.cs and why each operation belongs there. Show CreateScore and explain why the same product gets different scores. Run DeferredTests and explain the counter before iteration and after materialisation.

Show the C export, its header, the DllImport declaration, the adapter and the 1750 cent native probe. Explain why the source array is safe after its input file is deleted and why the failure workflow preserves the previous export.

For a small live change, adjust a scoring weight or a report filter and update the corresponding expected assertion before rerunning the test runner. Be ready to explain the change independently. The assignment awards eight points for implementation and seven for individual defense, so functioning code does not replace the required defense.

Submission archive

powershell -NoProfile -ExecutionPolicy Bypass -File scripts/package.ps1

Run packaging from the Git checkout with Git installed. This uses git archive to package the committed HEAD into artifacts/IFP-assignment7.zip. It includes source, project files, scripts, tests, README and sample data. It excludes generated bin and obj folders, IDE files, the downloaded compiler and native build outputs. A reproducible native build procedure is included. Commit any intended changes before packaging. The extracted submission can build and run without Git.

Upload the ZIP to Moodle according to the course deadline. A GitHub repository link does not replace the ZIP submission.

References

Microsoft Learn Platform Invoke documentation
https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke

Official Zig releases and download metadata
https://ziglang.org/download/
https://ziglang.org/download/index.json
