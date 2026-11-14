#include "inventory_native.h"

int32_t inv_handling_fee_cents(int32_t unit_price_cents, int32_t quantity)
{
    if (unit_price_cents < 0 || unit_price_cents > INV_MAX_UNIT_PRICE_CENTS)
        return INV_ERR_PRICE_RANGE;
    if (quantity < 0 || quantity > INV_MAX_QUANTITY)
        return INV_ERR_QUANTITY_RANGE;
    if (quantity == 0)
        return 0;

    int64_t value = (int64_t)unit_price_cents * (int64_t)quantity; /* max 1e12: fits int64 */
    int64_t fee = (value * 2 + 50) / 100;                          /* 2%, rounded half up  */

    if (fee < INV_MIN_FEE_CENTS)
        fee = INV_MIN_FEE_CENTS;
    if (fee > INV_MAX_FEE_CENTS)
        fee = INV_MAX_FEE_CENTS;
    return (int32_t)fee;
}