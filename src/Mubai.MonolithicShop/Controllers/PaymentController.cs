using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Services;

namespace Mubai.MonolithicShop.Controllers;

/// <summary>
/// 支付查询接口，按订单追踪支付进度。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// 根据订单编号获取支付结果。
    /// </summary>
    [HttpGet("{orderId:long}")]
    public async Task<ActionResult<PaymentResponseDto>> GetByOrder(long orderId, CancellationToken token)
    {
        var payment = await _paymentService.GetByOrderAsync(orderId, token);
        return payment is null ? NotFound() : Ok(payment);
    }
}
