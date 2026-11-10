#include <assert.h>
#include <stdio.h>
#include "inventory_native.h"

int main(void)
{
    assert(inventory_restock_cost(3, 10, 250) == 1750);
    assert(inventory_restock_cost(10, 10, 250) == 0);
    assert(inventory_restock_cost(12, 10, 250) == 0);
    assert(inventory_restock_cost(-1, 10, 250) == -1);
    assert(inventory_restock_cost(0, 1000001, 1) == -1);
    assert(inventory_restock_cost(0, 1, 1000001) == -1);
    assert(inventory_restock_cost(0, 1000000, 1000000) == INT64_C(1000000000000));
    puts("Native C assertions passed");
    return 0;
}
