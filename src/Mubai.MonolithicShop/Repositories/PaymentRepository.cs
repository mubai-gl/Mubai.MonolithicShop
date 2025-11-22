using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public class PaymentRepository(ShopDbContext dbContext) : GenericRepository<Payment, Guid>(dbContext), IPaymentRepository
{
    public Task<Payment?> GetByOrderIdAsync(long orderId, CancellationToken token = default) =>
        DbSet.Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.OrderId == orderId, token);
}
