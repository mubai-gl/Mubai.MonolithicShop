using Microsoft.AspNetCore.Mvc;
using Mubai.MonolithicShop.Dtos.Identity;
using Mubai.MonolithicShop.Services;

namespace Mubai.MonolithicShop.Controllers;

/// <summary>
/// 身份认证接口，提供登录、刷新令牌与注册等功能。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, IUserService userService) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly IUserService _userService = userService;

    /// <summary>
    /// 使用账号密码登录，返回访问令牌与刷新令牌。
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginDto request, CancellationToken token)
    {
        var response = await _authService.LoginAsync(request, token);
        return Ok(response);
    }

    /// <summary>
    /// 通过刷新令牌换取新的访问令牌。
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshTokenDto request, CancellationToken token)
    {
        var response = await _authService.RefreshAsync(request.RefreshToken, token);
        return Ok(response);
    }

    /// <summary>
    /// 注册新用户，提供统一的前后端入口。
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<UserResponseDto>> Register(CreateUserDto request, CancellationToken token)
    {
        await _userService.RegisterAsync(request, token);
        return Ok();
    }
}
