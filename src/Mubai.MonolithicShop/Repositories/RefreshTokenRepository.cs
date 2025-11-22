using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public class RefreshTokenRepository(ShopDbContext dbContext) : GenericRepository<RefreshToken, Guid>(dbContext), IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
}
