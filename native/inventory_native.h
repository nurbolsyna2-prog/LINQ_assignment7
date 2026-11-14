#ifndef INVENTORY_NATIVE_H
#define INVENTORY_NATIVE_H

#include <stdint.h>

#if defined(_WIN32)
#define INV_EXPORT __declspec(dllexport)
#else
#define INV_EXPORT __attribute__((visibility("default")))
#endif

#define INV_MAX_UNIT_PRICE_CENTS 10000000 /* valid unit price: 0..10,000,000 cents */
#define INV_MAX_QUANTITY 100000           /* valid quantity:   0..100,000 units    */
#define INV_MIN_FEE_CENTS 150             /* minimum fee when quantity > 0         */
#define INV_MAX_FEE_CENTS 50000           /* fee cap                               */

#define INV_ERR_PRICE_RANGE (-1)
#define INV_ERR_QUANTITY_RANGE (-2)

#ifdef __cplusplus
extern "C"
{
#endif

    /* fee = 2% of (price * quantity), rounded half up, min 150, max 50,000 cents;
       0 when quantity == 0.  Negative return value = INV_ERR_* code. */
    INV_EXPORT int32_t inv_handling_fee_cents(int32_t unit_price_cents, int32_t quantity);

#ifdef __cplusplus
}
#endif
#endif