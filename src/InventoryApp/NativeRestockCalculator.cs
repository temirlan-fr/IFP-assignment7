using System.Runtime.InteropServices;

namespace InventoryApp;

public sealed class NativeRestockCalculator
{
    private readonly Func<int, int, int, long> invoke;
    public NativeRestockCalculator() : this(NativeMethods.RestockCost) { }

    // Injection is used only for controlled boundary failure tests and demonstrations.
    public NativeRestockCalculator(Func<int, int, int, long> invoke) => this.invoke = invoke;

    public Result<long> Calculate(int quantity, int reorderLevel, int unitPriceCents)
    {
        if (quantity is < 0 or > 1_000_000 || reorderLevel is < 0 or > 1_000_000 || unitPriceCents is < 0 or > 1_000_000)
            return Result<long>.Failure("InvalidNativeInput", "Native inputs must be between 0 and 1000000.");
        try
        {
            var cents = invoke(quantity, reorderLevel, unitPriceCents);
            return cents is < 0 or > 1_000_000_000_000L
                ? Result<long>.Failure("NativeRejected", "Native library returned an invalid cost.")
                : Result<long>.Success(cents);
        }
        catch (DllNotFoundException)
        { return Result<long>.Failure("NativeUnavailable", "inventory_native.dll could not be loaded. Build the Windows x64 native component."); }
        catch (EntryPointNotFoundException)
        { return Result<long>.Failure("NativeEntryPoint", "inventory_restock_cost was not found. Rebuild the matching native library."); }
        catch (BadImageFormatException)
        { return Result<long>.Failure("NativeArchitecture", "Native library must match the Windows x64 application."); }
    }

    private static class NativeMethods
    {
        [DllImport("inventory_native.dll", EntryPoint = "inventory_restock_cost",
            ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        internal static extern long RestockCost(int quantity, int reorderLevel, int unitPriceCents);
    }
}
