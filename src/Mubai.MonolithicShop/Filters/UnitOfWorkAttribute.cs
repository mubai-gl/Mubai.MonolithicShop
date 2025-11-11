using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Mubai.MonolithicShop.Infrastructure;

namespace Mubai.MonolithicShop.Filters;

public sealed class UnitOfWorkAttribute : TypeFilterAttribute
{
    public UnitOfWorkAttribute() : base(typeof(UnitOfWorkFilter))
    {
    }
}

public class UnitOfWorkFilter : IAsyncActionFilter
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnitOfWorkFilter> _logger;

    public UnitOfWorkFilter(IUnitOfWork unitOfWork, ILogger<UnitOfWorkFilter> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await _unitOfWork.BeginTransactionAsync(context.HttpContext.RequestAborted);

        var executedContext = await next();

        if (executedContext.Exception is null || executedContext.ExceptionHandled)
        {
            await _unitOfWork.CommitAsync(context.HttpContext.RequestAborted);
        }
        else
        {
            _logger.LogWarning(executedContext.Exception, "Rolling back transaction for request {TraceId}", context.HttpContext.TraceIdentifier);
            await _unitOfWork.RollbackAsync();
        }
    }
}
