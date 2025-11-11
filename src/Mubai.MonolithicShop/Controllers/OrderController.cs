using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Filters;
using Mubai.MonolithicShop.Services;

namespace Mubai.MonolithicShop.Controllers;

/// <summary>
/// 订单接口，提供下单与订单查询能力。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[UnitOfWork]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// 创建新订单并返回订单详情。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> PlaceOrder(PlaceOrderRequestDto request, CancellationToken token)
    {
        var order = await _orderService.PlaceOrderAsync(request, token);
        return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, order);
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
}
