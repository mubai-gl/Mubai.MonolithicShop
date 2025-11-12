using Mubai.MonolithicShop.Dtos;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 库存服务契约。
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// 获取库存快照。
    /// </summary>
    Task<IReadOnlyCollection<InventoryItemResponseDto>> GetInventoryAsync(CancellationToken token = default);

    /// <summary>
    /// 调整商品库存。
    /// </summary>
    Task<InventoryItemResponseDto> AdjustInventoryAsync(AdjustInventoryRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 为订单尝试锁定库存。
    /// </summary>
    Task<InventoryReservationResultDto> TryReserveStockAsync(IEnumerable<OrderItemRequestDto> items, CancellationToken token = default);

    /// <summary>
    /// 释放未使用的库存预留。
    /// </summary>
    Task ReleaseReservationAsync(IEnumerable<OrderItemRequestDto> items, CancellationToken token = default);

    /// <summary>
    /// 将预留库存转为正式扣减。
    /// </summary>
    Task CommitReservationAsync(IEnumerable<OrderItemRequestDto> items, CancellationToken token = default);
}
