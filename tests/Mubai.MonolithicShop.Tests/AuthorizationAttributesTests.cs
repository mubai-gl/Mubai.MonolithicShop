using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Mubai.MonolithicShop.Controllers;

namespace Mubai.MonolithicShop.Tests;

public class AuthorizationAttributesTests
{
    [Theory]
    [InlineData(typeof(InventoryController))]
    [InlineData(typeof(OrderController))]
    [InlineData(typeof(PaymentController))]
    [InlineData(typeof(ProductController))]
    [InlineData(typeof(UserController))]
    public void Controller_ShouldRequireAuthorization(Type controllerType)
    {
        var hasAuthorizeAttribute = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Any();

        hasAuthorizeAttribute.Should().BeTrue($"{controllerType.Name} must be protected by [Authorize]");
    }
}
