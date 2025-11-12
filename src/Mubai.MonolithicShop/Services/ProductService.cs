using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Repositories;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 商品服务，负责商品的增删改查及初始库存建立。
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// 创建商品并初始化库存记录。
    /// </summary>
    public async Task<ProductResponseDto> CreateAsync(CreateProductRequestDto request, CancellationToken token = default)
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

        await _unitOfWork.SaveChangesAsync(token);
        return Map(product);
    }

    /// <summary>
    /// 更新商品信息，同时校验 SKU 是否被占用。
    /// </summary>
    public async Task<ProductResponseDto> UpdateAsync(UpdateProductRequestDto request, CancellationToken token = default)
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

        return Map(product);
    }

    /// <summary>
    /// 获取全部商品列表。
    /// </summary>
    public async Task<IReadOnlyCollection<ProductResponseDto>> GetAllAsync(CancellationToken token = default)
    {
        var products = await _productRepository.ListAsync(token);
        return products.Select(Map).ToList();
    }

    /// <summary>
    /// 根据主键获取单个商品。
    /// </summary>
    public async Task<ProductResponseDto?> GetAsync(Guid id, CancellationToken token = default)
    {
        var product = await _productRepository.GetByIdAsync(id, token);
        return product is null ? null : Map(product);
    }

    private static ProductResponseDto Map(Product product) =>
        new(product.Id, product.Name, product.Sku, product.Price, product.Description, product.IsActive);
}
