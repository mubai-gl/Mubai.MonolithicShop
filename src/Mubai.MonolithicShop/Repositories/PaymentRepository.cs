using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public class PaymentRepository : GenericRepository<Payment, Guid>, IPaymentRepository
{
    public PaymentRepository(ShopDbContext dbContext) : base(dbContext)
    {
    }

    public Task<Payment?> GetByOrderIdAsync(long orderId, CancellationToken token = default) =>
        DbSet.Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.OrderId == orderId, token);
}
