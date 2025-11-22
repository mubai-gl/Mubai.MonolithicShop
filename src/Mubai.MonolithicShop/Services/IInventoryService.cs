using Mubai.MonolithicShop.Dtos.Inventory;

namespace Mubai.MonolithicShop.Services;

public interface IInventoryService
{
    /// <summary>
    /// 获取库存列表。
    /// </summary>
    Task<IReadOnlyCollection<InventoryItemResponseDto>> GetInventoryAsync(CancellationToken token = default);
}
