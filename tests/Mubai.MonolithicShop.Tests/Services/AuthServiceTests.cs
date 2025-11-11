using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;
using System.Linq;

namespace Mubai.MonolithicShop.Tests.Services;

public class AuthServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ShouldThrow_WhenPasswordDoesNotMatch()
    {
        await _factory.ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var authService = services.GetRequiredService<IAuthService>();

        var email = "auth-tests@example.com";
        await SeedUserAsync(userManager, email, "Correct#123");

        var act = () => authService.LoginAsync(new LoginRequestDto(email, "WrongPassword!"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Refresh_ShouldThrow_WhenTokenNotFound()
    {
        await _factory.ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var act = () => authService.RefreshAsync(Guid.NewGuid().ToString("N"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static async Task SeedUserAsync(UserManager<ApplicationUser> userManager, string email, string password)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };

        var result = await userManager.CreateAsync(user, password);
        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.Description)));
    }
}
