using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Services;

namespace Mubai.MonolithicShop.Controllers;

/// <summary>
/// 库存接口，提供库存查询与调整能力。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// 获取所有库存商品的数量概览。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemResponseDto>>> GetAll(CancellationToken token)
    {
        var items = await _inventoryService.GetInventoryAsync(token);
        return Ok(items);
    }

    /// <summary>
    /// 按商品调整库存数量，可执行增减。
    /// </summary>
    [HttpPost("adjust")]
    public async Task<ActionResult<InventoryItemResponseDto>> Adjust(AdjustInventoryRequestDto request, CancellationToken token)
    {
        var updated = await _inventoryService.AdjustInventoryAsync(request, token);
        return Ok(updated);
    }
}
