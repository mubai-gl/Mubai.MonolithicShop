using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public class ProductRepository : GenericRepository<Product, Guid>, IProductRepository
{
    public ProductRepository(ShopDbContext dbContext) : base(dbContext)
    {
    }

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken token = default) =>
        DbSet.FirstOrDefaultAsync(p => p.Sku == sku, token);
}
