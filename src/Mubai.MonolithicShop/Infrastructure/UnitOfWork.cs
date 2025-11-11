using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mubai.MonolithicShop.Infrastructure;

public interface IUnitOfWork : IAsyncDisposable
{
    ShopDbContext DbContext { get; }
    Task BeginTransactionAsync(CancellationToken token = default);
    Task CommitAsync(CancellationToken token = default);
    Task RollbackAsync();
    Task<int> SaveChangesAsync(CancellationToken token = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken token = default);
}

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ShopDbContext _dbContext;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(ShopDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ShopDbContext DbContext => _dbContext;

    public async Task BeginTransactionAsync(CancellationToken token = default)
    {
        if (_currentTransaction is not null || !_dbContext.Database.IsRelational())
        {
            return;
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(token);
    }

    public async Task CommitAsync(CancellationToken token = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.CommitAsync(token);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackAsync()
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.RollbackAsync();
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public Task<int> SaveChangesAsync(CancellationToken token = default) => _dbContext.SaveChangesAsync(token);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken token = default)
    {
        await BeginTransactionAsync(token);
        try
        {
            await operation(token);
            await CommitAsync(token);
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
        }

        await _dbContext.DisposeAsync();
    }
}
