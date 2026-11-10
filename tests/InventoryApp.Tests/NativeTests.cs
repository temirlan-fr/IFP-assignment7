using static InventoryApp.Tests.Check;

namespace InventoryApp.Tests;

internal static class NativeTests
{
    public static IEnumerable<(string, Action)> Cases()
    {
        yield return ("Real native DLL returns manually calculated costs and boundary values", () =>
        {
            var native = new NativeRestockCalculator();
            Equal(1750L, native.Calculate(3, 10, 250).Value);
            Equal(0L, native.Calculate(12, 10, 250).Value);
            Equal(1_000_000_000_000L, native.Calculate(0, 1_000_000, 1_000_000).Value);
        });
        yield return ("Native adapter validates inputs before entering native code", () =>
        {
            var calls = 0;
            var native = new NativeRestockCalculator((q, r, p) => { calls++; return 0; });
            Equal("InvalidNativeInput", native.Calculate(-1, 10, 250).Error!.Code);
            Equal("InvalidNativeInput", native.Calculate(0, 1_000_001, 250).Error!.Code);
            Equal("InvalidNativeInput", native.Calculate(0, 10, -1).Error!.Code);
            Equal(0, calls);
        });
        yield return ("Native adapter converts loading binding and architecture failures", () =>
        {
            Equal("NativeUnavailable", new NativeRestockCalculator((q, r, p) => throw new DllNotFoundException()).Calculate(3, 10, 250).Error!.Code);
            Equal("NativeEntryPoint", new NativeRestockCalculator((q, r, p) => throw new EntryPointNotFoundException()).Calculate(3, 10, 250).Error!.Code);
            Equal("NativeArchitecture", new NativeRestockCalculator((q, r, p) => throw new BadImageFormatException()).Calculate(3, 10, 250).Error!.Code);
            Equal("NativeRejected", new NativeRestockCalculator((q, r, p) => -1).Calculate(3, 10, 250).Error!.Code);
        });
    }
}
