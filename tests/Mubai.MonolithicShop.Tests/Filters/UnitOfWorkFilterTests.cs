using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Mubai.MonolithicShop;
using Mubai.MonolithicShop.Filters;
using Mubai.MonolithicShop.Infrastructure;

namespace Mubai.MonolithicShop.Tests.Filters;

public class UnitOfWorkFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_ShouldCommit_WhenActionSucceeds()
    {
        var unitOfWork = new FakeUnitOfWork();
        var filter = new UnitOfWorkFilter(unitOfWork, NullLogger<UnitOfWorkFilter>.Instance);
        var (executingContext, actionContext, filterMetadata, controller) = CreateContext();

        ActionExecutionDelegate next = () =>
        {
            var executed = new ActionExecutedContext(actionContext, filterMetadata, controller);
            return Task.FromResult(executed);
        };

        await filter.OnActionExecutionAsync(executingContext, next);

        unitOfWork.BeginCalled.Should().BeTrue();
        unitOfWork.CommitCalled.Should().BeTrue();
        unitOfWork.RollbackCalled.Should().BeFalse();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldRollback_WhenActionThrows()
    {
        var unitOfWork = new FakeUnitOfWork();
        var filter = new UnitOfWorkFilter(unitOfWork, NullLogger<UnitOfWorkFilter>.Instance);
        var (executingContext, actionContext, filterMetadata, controller) = CreateContext();

        ActionExecutionDelegate next = () =>
        {
            var executed = new ActionExecutedContext(actionContext, filterMetadata, controller)
            {
                Exception = new InvalidOperationException("boom"),
                ExceptionHandled = false
            };
            return Task.FromResult(executed);
        };

        await filter.OnActionExecutionAsync(executingContext, next);

        unitOfWork.BeginCalled.Should().BeTrue();
        unitOfWork.CommitCalled.Should().BeFalse();
        unitOfWork.RollbackCalled.Should().BeTrue();
    }

    private static (ActionExecutingContext executingContext, ActionContext actionContext, IList<IFilterMetadata> filters, object controller) CreateContext()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var actionArguments = new Dictionary<string, object?>();
        var controller = new object();
        var executingContext = new ActionExecutingContext(actionContext, filters, actionArguments, controller);
        return (executingContext, actionContext, filters, controller);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool BeginCalled { get; private set; }
        public bool CommitCalled { get; private set; }
        public bool RollbackCalled { get; private set; }

        public ShopDbContext DbContext => null!;

        public Task BeginTransactionAsync(CancellationToken token = default)
        {
            BeginCalled = true;
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken token = default)
        {
            CommitCalled = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken token = default) =>
            operation(token);

        public Task RollbackAsync()
        {
            RollbackCalled = true;
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken token = default) => Task.FromResult(0);
    }
}
