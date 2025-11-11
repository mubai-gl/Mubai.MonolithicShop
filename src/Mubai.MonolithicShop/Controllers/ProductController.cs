using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Filters;
using Mubai.MonolithicShop.Services;

namespace Mubai.MonolithicShop.Controllers;

/// <summary>
/// 商品接口，支持查询、创建与更新。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[UnitOfWork]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// 获取所有商品列表。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetAll(CancellationToken token)
    {
        var products = await _productService.GetAllAsync(token);
        return Ok(products);
    }

    /// <summary>
    /// 根据主键获取商品详情。
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponseDto>> Get(Guid id, CancellationToken token)
    {
        var product = await _productService.GetAsync(id, token);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>
    /// 创建新商品。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> Create(CreateProductRequestDto request, CancellationToken token)
    {
        var created = await _productService.CreateAsync(request, token);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>
    /// 更新商品基本信息。
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponseDto>> Update(Guid id, UpdateProductRequestDto request, CancellationToken token)
    {
        if (id != request.Id)
        {
            return BadRequest("路由中的主键与请求体不一致，请确认参数。");
        }

        var updated = await _productService.UpdateAsync(request, token);
        return Ok(updated);
    }
}
