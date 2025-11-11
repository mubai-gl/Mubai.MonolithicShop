using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public interface IOrderRepository : IGenericRepository<Order, long>
{
    Task<Order?> GetWithItemsAsync(long orderId, CancellationToken token = default);
}
