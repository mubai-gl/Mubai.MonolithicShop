using Mubai.MonolithicShop.Dtos.Product;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Repositories;
using Mubai.UnitOfWork.Abstractions;
using Mubai.UnitOfWork.EntityFrameworkCore;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 商品服务。
/// </summary>
public class ProductService(
    IProductRepository productRepository,
    IInventoryRepository inventoryRepository,
    IUnitOfWork<ShopDbContext> unitOfWork) : IProductService
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;
    private readonly IUnitOfWork<ShopDbContext> _unitOfWork = unitOfWork;

    /// <inheritdoc />
    public async Task CreateAsync(CreateProductRequestDto request, CancellationToken token = default)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var existing = await _productRepository.GetBySkuAsync(request.Sku, token);
            if (existing is not null)
            {
                throw new InvalidOperationException($"SKU {request.Sku} 已存在，无法重复创建。");
            }

            var product = new Product
            {
                Name = request.Name,
                Sku = request.Sku,
                Price = request.Price,
                Description = request.Description
            };

            await _productRepository.AddAsync(product, token);
            await _inventoryRepository.AddAsync(new InventoryItem
            {
                ProductId = product.Id
            }, token);

            await _unitOfWork.SaveChangesAsync(ct);
        }, token);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(UpdateProductRequestDto request, CancellationToken token = default)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, token)
                      ?? throw new KeyNotFoundException("未找到指定商品，无法更新。");

        if (!string.Equals(product.Sku, request.Sku, StringComparison.OrdinalIgnoreCase))
        {
            var conflict = await _productRepository.GetBySkuAsync(request.Sku, token);
            if (conflict is not null && conflict.Id != product.Id)
            {
                throw new InvalidOperationException("该 SKU 已绑定其他商品。");
            }
        }

        product.Name = request.Name;
        product.Sku = request.Sku;
        product.Price = request.Price;
        product.Description = request.Description;
        product.IsActive = request.IsActive;
        product.UpdatedTime = DateTime.UtcNow;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ProductResponseDto>> GetAllAsync(CancellationToken token = default)
    {
        var products = await _productRepository.ListAsync(token);
        return products.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> GetAsync(Guid id, CancellationToken token = default)
    {
        var product = await _productRepository.GetByIdAsync(id, token);
        return product is null ? null : Map(product);
    }

    /// <summary>
    /// 实体到响应模型的映射逻辑。
    /// </summary>
    private static ProductResponseDto Map(Product product) =>
        new(product.Id, product.Name, product.Sku, product.Price, product.Description, product.IsActive);
}
