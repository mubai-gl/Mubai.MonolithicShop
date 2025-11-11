using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public interface IProductRepository : IGenericRepository<Product, Guid>
{
    Task<Product?> GetBySkuAsync(string sku, CancellationToken token = default);
}
