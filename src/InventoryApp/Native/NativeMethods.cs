using System.Runtime.InteropServices;

namespace InventoryApp.Native;

/// <summary>
///   C : int32_t inv_handling_fee_cents(int32_t unit_price_cents, int32_t quantity)
///   C#: int HandlingFeeCents(int, int)
/// </summary>
internal static class NativeMethods
{
    [DllImport("inventory_native", EntryPoint = "inv_handling_fee_cents",
               CallingConvention = CallingConvention.Cdecl)]
    internal static extern int HandlingFeeCents(int unitPriceCents, int quantity);
}