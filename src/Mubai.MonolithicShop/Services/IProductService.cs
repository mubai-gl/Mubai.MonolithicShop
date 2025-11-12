using Mubai.MonolithicShop.Dtos;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 商品服务契约。
/// </summary>
public interface IProductService
{
    /// <summary>
    /// 创建商品。
    /// </summary>
    Task<ProductResponseDto> CreateAsync(CreateProductRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 更新商品。
    /// </summary>
    Task<ProductResponseDto> UpdateAsync(UpdateProductRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 获取全部商品。
    /// </summary>
    Task<IReadOnlyCollection<ProductResponseDto>> GetAllAsync(CancellationToken token = default);

    /// <summary>
    /// 根据主键获取商品。
    /// </summary>
    Task<ProductResponseDto?> GetAsync(Guid id, CancellationToken token = default);
}
