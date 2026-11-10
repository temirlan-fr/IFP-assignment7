#include "inventory_native.h"

INVENTORY_API int64_t INVENTORY_CALL inventory_restock_cost(
    int32_t quantity, int32_t reorder_level, int32_t unit_price_cents)
{
    if (quantity < 0 || quantity > 1000000 ||
        reorder_level < 0 || reorder_level > 1000000 ||
        unit_price_cents < 0 || unit_price_cents > 1000000)
        return -1;

    if (quantity >= reorder_level)
        return 0;

    return (int64_t)(reorder_level - quantity) * unit_price_cents;
}
