using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Dtos.Order;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 订单业务接口。
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// 提交订单
    /// </summary>
    Task PlaceOrderAsync(PlaceOrderRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 根据订单编号获取订单详情。
    /// </summary>
    Task<OrderResponseDto?> GetAsync(long orderId, CancellationToken token = default);
}
