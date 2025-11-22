using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mubai.MonolithicShop.Dtos.Order;
using Mubai.MonolithicShop.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mubai.MonolithicShop.Controllers;

/// <summary>
/// 订单接口，提供下单与订单查询能力。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController(IOrderService orderService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;

    /// <summary>
    /// 创建新订单并返回订单详情。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> PlaceOrder(PlaceOrderRequestDto request, CancellationToken token)
    {
        if (!TryResolveUserId(out var userId))
        {
            return Unauthorized("缺少有效的用户凭证");
        }

        var normalizedRequest = request with { UserId = userId };
        await _orderService.PlaceOrderAsync(normalizedRequest, token);
        return Ok();
    }

    /// <summary>
    /// 根据订单主键获取订单信息。
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderResponseDto>> GetById(long id, CancellationToken token)
    {
        var order = await _orderService.GetAsync(id, token);
        return order is null ? NotFound() : Ok(order);
    }
    private bool TryResolveUserId(out Guid userId)
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(claimValue, out userId);
    }
}
