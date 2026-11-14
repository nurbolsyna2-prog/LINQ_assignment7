using InventoryApp.Domain;

namespace InventoryApp.Native;

public interface IFeeCalculator
{
    Result<int> HandlingFeeCents(int unitPriceCents, int quantity);
}

/// <summary>
/// Adapter around the native call. Validates domain input first, then calls C, and translates
/// loading / binding failures and native error codes into explicit Result failures.
/// </summary>
public sealed class NativeFeeCalculator : IFeeCalculator
{
    public const int MaxQuantity = 100_000;

    private readonly Func<int, int, int> _native;

    public NativeFeeCalculator() : this(NativeMethods.HandlingFeeCents) { }

    /// <summary>Injectable binding so tests can point the adapter at a missing library.</summary>
    public NativeFeeCalculator(Func<int, int, int> native) => _native = native;

    public Result<int> HandlingFeeCents(int unitPriceCents, int quantity)
    {
        if (unitPriceCents < 0 || unitPriceCents > Item.MaxUnitPriceCents)
            return Result.Fail<int>(ErrorCode.InvalidData, $"Unit price {unitPriceCents} is outside 0..{Item.MaxUnitPriceCents}.");
        if (quantity < 0 || quantity > MaxQuantity)
            return Result.Fail<int>(ErrorCode.InvalidData, $"Quantity {quantity} is outside 0..{MaxQuantity}.");

        try
        {
            int fee = _native(unitPriceCents, quantity);
            return fee < 0
                ? Result.Fail<int>(ErrorCode.NativeRejectedInput, $"Native library rejected the input (code {fee}).")
                : Result.Ok(fee);
        }
        catch (DllNotFoundException ex)
        {
            return Result.Fail<int>(ErrorCode.NativeLoadFailed, "Native library could not be loaded: " + ex.Message);
        }
        catch (BadImageFormatException ex)
        {
            return Result.Fail<int>(ErrorCode.NativeLoadFailed, "Native library has the wrong architecture: " + ex.Message);
        }
        catch (EntryPointNotFoundException ex)
        {
            return Result.Fail<int>(ErrorCode.NativeBindingFailed, "Native export not found: " + ex.Message);
        }
    }
}