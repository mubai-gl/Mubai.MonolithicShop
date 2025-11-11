using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public interface IInventoryRepository : IGenericRepository<InventoryItem, Guid>
{
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken token = default);
    Task<IReadOnlyCollection<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken token = default);
}
