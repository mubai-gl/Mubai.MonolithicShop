using Mubai.MonolithicShop.Dtos.Payment;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 支付服务契约。
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// 处理支付结果.
    /// </summary>
    Task ProcessPaymentAsync(ProcessPaymentRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 根据订单编号查询支付记录。
    /// </summary>
    Task<PaymentResponseDto?> GetByOrderAsync(long orderId, CancellationToken token = default);
}
