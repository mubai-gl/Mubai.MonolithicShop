using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public class ProductRepository(ShopDbContext dbContext) : GenericRepository<Product, Guid>(dbContext), IProductRepository
{
    public Task<Product?> GetBySkuAsync(string sku, CancellationToken token = default) =>
        DbSet.FirstOrDefaultAsync(p => p.Sku == sku, token);
}
