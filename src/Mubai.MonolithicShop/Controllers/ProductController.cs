using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mubai.MonolithicShop.Dtos.Product;
using Mubai.MonolithicShop.Services;

namespace Mubai.MonolithicShop.Controllers;

/// <summary>
/// 商品接口。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

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
    public async Task<ActionResult> Create(CreateProductRequestDto request, CancellationToken token)
    {
        await _productService.CreateAsync(request, token);
        return Ok();
    }

    /// <summary>
    /// 更新商品基本信息。
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ProductResponseDto>> Update(UpdateProductRequestDto request, CancellationToken token)
    {
        await _productService.UpdateAsync(request, token);
        return Ok();
    }
}
