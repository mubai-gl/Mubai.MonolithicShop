using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public class InventoryRepository : GenericRepository<InventoryItem, Guid>, IInventoryRepository
{
    public InventoryRepository(ShopDbContext dbContext) : base(dbContext)
    {
    }

    public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken token = default) =>
        DbSet.Include(i => i.Product).FirstOrDefaultAsync(i => i.ProductId == productId, token);

    public async Task<IReadOnlyCollection<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken token = default)
    {
        var ids = productIds.ToArray();
        return await DbSet.Include(i => i.Product)
            .Where(i => ids.Contains(i.ProductId))
            .ToListAsync(token);
    }
}
