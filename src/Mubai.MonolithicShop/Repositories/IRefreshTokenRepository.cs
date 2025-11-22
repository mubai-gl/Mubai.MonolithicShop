using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Repositories;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken, Guid>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
}
