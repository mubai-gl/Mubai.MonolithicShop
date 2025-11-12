using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Mubai.MonolithicShop.Filters;

namespace Mubai.MonolithicShop.Tests.Filters;

public class ApiExceptionFilterTests
{
    [Fact]
    public void OnException_ShouldWrapExceptionIntoProblemDetails()
    {
        var filter = new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance);
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new InvalidOperationException("手动触发异常")
        };

        filter.OnException(context);

        context.ExceptionHandled.Should().BeTrue();
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var problem = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("手动触发异常");
        problem.Status.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
