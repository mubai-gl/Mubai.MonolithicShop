using Mubai.MonolithicShop.Dtos.Product;
namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 商品服务契约。
/// </summary>
public interface IProductService
{
    /// <summary>
    /// 创建新的商品。
    /// </summary>
    Task CreateAsync(CreateProductRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 更新商品的基本信息。
    /// </summary>
    Task UpdateAsync(UpdateProductRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 获取全部商品列表。
    /// </summary>
    Task<IReadOnlyCollection<ProductResponseDto>> GetAllAsync(CancellationToken token = default);

    /// <summary>
    /// 根据唯一标识获取单个商品。
    /// </summary>
    Task<ProductResponseDto?> GetAsync(Guid id, CancellationToken token = default);
}
