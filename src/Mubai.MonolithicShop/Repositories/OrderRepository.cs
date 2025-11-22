using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public class OrderRepository(ShopDbContext dbContext) : GenericRepository<Order, long>(dbContext), IOrderRepository
{
    public Task<Order?> GetWithItemsAsync(long orderId, CancellationToken token = default) =>
        DbSet.Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, token);
}
