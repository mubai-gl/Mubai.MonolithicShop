using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Dtos.Identity;
using Mubai.MonolithicShop.Entities;
using Mubai.UnitOfWork.Abstractions;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 用户服务
/// </summary>
public class UserService(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork) : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <inheritdoc />
    public async Task RegisterAsync(CreateUserDto request, CancellationToken token = default)
    {
        var existing = await _userManager.Users.AnyAsync(u => u.Email == request.Email, token);

        if (existing)
        {
            throw new InvalidOperationException("用户已存在。");
        }

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            DisplayName = request.Name,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(",", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"创建用户失败: {errors}");
        }

        await _unitOfWork.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public async Task<UserResponseDto?> GetAsync(Guid id, CancellationToken token = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user is null ? null : Map(user);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<UserResponseDto>> GetAllAsync(CancellationToken token = default)
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync(token);
        return users.Select(Map).ToList();
    }

    /// <summary>
    /// 将实体映射为用户响应模型。
    /// </summary>
    private static UserResponseDto Map(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName ?? user.UserName ?? string.Empty, user.PhoneNumber);
}
