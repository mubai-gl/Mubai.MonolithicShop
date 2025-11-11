using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken, Guid>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ShopDbContext dbContext) : base(dbContext)
    {
    }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
}
