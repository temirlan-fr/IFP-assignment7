#ifndef INVENTORY_NATIVE_H
#define INVENTORY_NATIVE_H

#include <stdint.h>

#if defined(_WIN32)
#define INVENTORY_API __declspec(dllexport)
#define INVENTORY_CALL __cdecl
#else
#define INVENTORY_API
#define INVENTORY_CALL
#endif

/* Return shortage times unit price in cents, or -1 for invalid input. */
INVENTORY_API int64_t INVENTORY_CALL inventory_restock_cost(
    int32_t quantity, int32_t reorder_level, int32_t unit_price_cents);

#endif
