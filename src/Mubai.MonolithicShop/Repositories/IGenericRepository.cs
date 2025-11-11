namespace Mubai.MonolithicShop.Repositories;

public interface IGenericRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken token = default);
    Task<List<TEntity>> ListAsync(CancellationToken token = default);
    Task AddAsync(TEntity entity, CancellationToken token = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    IQueryable<TEntity> Query();
}
