using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public interface IPaymentRepository : IGenericRepository<Payment, Guid>
{
    Task<Payment?> GetByOrderIdAsync(long orderId, CancellationToken token = default);
}
