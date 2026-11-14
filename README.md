# LINQ_assignment7
# InventoryApp

## Overview
Console app that loads inventory from CSV, produces two LINQ reports, scores
items with two curried configurations, and calls a small native C library to
compute a restock fee. All domain and boundary failures use `Result<T>` with
an explicit `AppError`; there are no exceptions escaping the domain.

## Requirements → Files
| Requirement | File / method |
|---|---|
| Extension #1 (compose Results) | `Extensions/ResultExtensions.cs` — `Bind`, `Map`, `GetOrElse` |
| Extension #2 (LINQ query) | `Extensions/ItemQueryExtensions.cs` — `LowStock`, `TotalValueByCategory` |
| Report 1 — low stock | `Reports/InventoryReports.LowStockReport` |
| Report 2 — value by category | `Reports/InventoryReports.ValueByCategoryReport` |
| Curried function | `Scoring/ItemScoring.CreateScore` |
| Partial applications | `QuantityFocusedScore`, `ValueFocusedScore` |
| Deferred execution | `Io/CsvItemSource.StreamItems` (`yield return`) |
| Materialisation | `Io/CsvItemSource.LoadAll` (`.ToList()`) |
| Resource ownership | `StreamReader` / `StreamWriter` in `Io/` — disposed with `using` |
| Native binding | `Native/NativeMethods.NativeRestockFeeCents` |
| Native adapter | `Native/NativeFeeCalculator` (validates + maps errors) |
| Workflow integration | `Workflow/InventoryWorkflow.Run` |
| Tests | `tests/InventoryApp.Tests/*` (22 tests) |

## Native function contract
- **Name:** `native_restock_fee_cents(int quantity, int unit_price_cents, int low_threshold)`
- **Inputs:** all `>= 0`; `low_threshold > 0`
- **Output:** restock fee in **cents** (`int`); `-1` on invalid input
- **Rule:** `qty >= threshold` → `0`; else `floor(unit_price_cents * (threshold - qty) * 5 / 100)`
- **Manual check:** `(8, 1250, 20)` → `750` cents
- **Platform:** Linux x64, gcc >= 11
- **Build:** `chmod +x native/build.sh && ./native/build.sh` → `runtime/linux-x64/libinventory_native.so`
- **Loading:** copied into app output by `InventoryApp.csproj`; P/Invoke name `inventory_native`
- **Integration difficulty:** P/Invoke resolves `inventory_native` to `libinventory_native.so` on Linux; the original binary was named differently, so we standardised the filename and added the csproj copy rule.
- **Failure handling:** `DllNotFoundException`, `EntryPointNotFoundException`, `BadImageFormatException` → `AppError` codes `NATIVE_LOAD`, `NATIVE_ENTRY`, `NATIVE_BAD_IMAGE`.

## Resource note (Task 6)
- `CsvItemSource.LoadAll` **materialises** into `List<Item>` once. Subsequent reports read from that snapshot; there is no repeated file read.
- `CsvItemSource.StreamItems` is deferred; each `foreach` re-runs the body (and reopens the file). It is only used by `LoadAll`, so the file is read exactly once per workflow run.
- `StreamReader` and `StreamWriter` are `using`-scoped: released on success, exception, or `break`.
- No long-lived caches or event subscriptions exist; nothing retains `Item` beyond the workflow call.
- **Trade-off:** snapshot uses memory proportional to input but guarantees no file handle stays open and no re-read; streaming uses O(1) memory but every enumeration reopens the file. We chose snapshot inside `LoadAll` because reports read the dataset multiple times.

## Build / run / test
Prereqs: .NET SDK 8.0, gcc (Linux/macOS).
```bash
./native/build.sh
dotnet build src/InventoryApp/InventoryApp.csproj
dotnet run --project src/InventoryApp/InventoryApp.csproj
dotnet test tests/InventoryApp.Tests/InventoryApp.Tests.csproj