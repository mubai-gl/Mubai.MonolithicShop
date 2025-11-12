using Mubai.MonolithicShop.Dtos;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 订单业务接口，定义下单与查询操作。
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// 提交订单并返回订单详情。
    /// </summary>
    Task<OrderResponseDto> PlaceOrderAsync(PlaceOrderRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 根据主键查询订单。
    /// </summary>
    Task<OrderResponseDto?> GetAsync(long orderId, CancellationToken token = default);
}
