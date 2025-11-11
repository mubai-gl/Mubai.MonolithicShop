using System.Threading;
using System.Threading.Tasks;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 支付服务契约。
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// 处理订单支付。
    /// </summary>
    Task<PaymentResponseDto> ProcessPaymentAsync(Order order, PaymentRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 根据订单编号查询支付记录。
    /// </summary>
    Task<PaymentResponseDto?> GetByOrderAsync(long orderId, CancellationToken token = default);
}
