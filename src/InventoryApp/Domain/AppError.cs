namespace InventoryApp.Domain;

public enum ErrorCode
{
    FileNotFound,
    FileUnreadable,
    MalformedContent,
    InvalidData,
    NativeLoadFailed,
    NativeBindingFailed,
    NativeRejectedInput,
    ExportFailed
}

public sealed record AppError(ErrorCode Code, string Message);