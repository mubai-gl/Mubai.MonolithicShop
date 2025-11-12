using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;

namespace Mubai.MonolithicShop.Tests.Services;

public class AuthServiceTests : DatabaseTestBase
{
    public AuthServiceTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_ShouldThrow_WhenPasswordDoesNotMatch()
    {
        await using var scope = CreateScope();
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
        await using var scope = CreateScope();
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
