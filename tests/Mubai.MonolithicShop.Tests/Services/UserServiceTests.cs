using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;
using System.Linq;

namespace Mubai.MonolithicShop.Tests.Services;

public class UserServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UserServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ShouldThrow_WhenEmailAlreadyExists()
    {
        await _factory.ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var userService = services.GetRequiredService<IUserService>();

        var email = "duplicate@example.com";
        await SeedUserAsync(userManager, email);

        var request = new CreateUserRequestDto(email, "重复用户", "Passw0rd!", "13800138000");

        var act = () => userService.RegisterAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Register_ShouldReturnUser_WhenPasswordValid()
    {
        await _factory.ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var request = new CreateUserRequestDto("new-user@example.com", "新用户", "Passw0rd!", "13900000000");

        var response = await userService.RegisterAsync(request, CancellationToken.None);

        response.Email.Should().Be(request.Email);
        response.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task Register_ShouldPropagateIdentityErrors()
    {
        await _factory.ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var request = new CreateUserRequestDto("weak-password@example.com", "弱口令用户", "123", null);

        var act = () => userService.RegisterAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*");
    }

    private static async Task SeedUserAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };

        var result = await userManager.CreateAsync(user, "Passw0rd!");
        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.Description)));
    }
}
