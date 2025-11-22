using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Dtos.Inventory;
using Mubai.MonolithicShop.Repositories;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 库存服务
/// </summary>
public class InventoryService(
    IInventoryRepository inventoryRepository) : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<InventoryItemResponseDto>> GetInventoryAsync(CancellationToken token = default)
    {
        return await _inventoryRepository
            .Query()
            .AsNoTracking()
            .Select(i => new InventoryItemResponseDto(
                i.ProductId,
                i.Product != null ? i.Product.Name : string.Empty,
                i.QuantityOnHand,
                i.ReservedQuantity))
            .ToListAsync(token);
    }
}
