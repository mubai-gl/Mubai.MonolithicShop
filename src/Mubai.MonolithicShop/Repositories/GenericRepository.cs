using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Infrastructure;

namespace Mubai.MonolithicShop.Repositories;

public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : class
{
    protected readonly ShopDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    protected GenericRepository(ShopDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken token = default)
    {
        return await DbSet.FindAsync([id], cancellationToken: token);
    }

    public virtual Task<List<TEntity>> ListAsync(CancellationToken token = default) =>
        DbSet.AsNoTracking().ToListAsync(token);

    public virtual async Task AddAsync(TEntity entity, CancellationToken token = default)
    {
        await DbSet.AddAsync(entity, token);
    }

    public virtual void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    public IQueryable<TEntity> Query() => DbSet.AsQueryable();
}
