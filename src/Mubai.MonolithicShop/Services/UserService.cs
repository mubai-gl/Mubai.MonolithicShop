using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 用户服务，提供注册与查询能力。
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// 注册用户，如果邮箱已存在则抛出异常。
    /// </summary>
    public async Task<UserResponseDto> RegisterAsync(CreateUserRequestDto request, CancellationToken token = default)
    {
        var existing = await _userManager.Users.AnyAsync(u => u.Email == request.Email, token);
        if (existing)
        {
            throw new InvalidOperationException("该邮箱已被注册，请更换后重试。");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.Name,
            PhoneNumber = request.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(";", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        await _unitOfWork.SaveChangesAsync(token);
        return Map(user);
    }

    /// <summary>
    /// 根据主键查询单个用户。
    /// </summary>
    public async Task<UserResponseDto?> GetAsync(Guid id, CancellationToken token = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user is null ? null : Map(user);
    }

    /// <summary>
    /// 获取全部用户列表。
    /// </summary>
    public async Task<IReadOnlyCollection<UserResponseDto>> GetAllAsync(CancellationToken token = default)
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync(token);
        return users.Select(Map).ToList();
    }

    private static UserResponseDto Map(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName ?? user.UserName ?? string.Empty, user.PhoneNumber);
}
