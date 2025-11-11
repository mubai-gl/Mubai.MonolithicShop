using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public class OrderRepository : GenericRepository<Order, long>, IOrderRepository
{
    public OrderRepository(ShopDbContext dbContext) : base(dbContext)
    {
    }

    public Task<Order?> GetWithItemsAsync(long orderId, CancellationToken token = default) =>
        DbSet.Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, token);
}
