using Microsoft.AspNetCore.Mvc;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Services;

namespace Mubai.MonolithicShop.Controllers;

/// <summary>
/// 身份认证接口，提供登录、刷新令牌以及公开注册能力。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    /// <summary>
    /// 使用账号密码登录并返回访问令牌和刷新令牌。
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginRequestDto request, CancellationToken token)
    {
        var response = await _authService.LoginAsync(request, token);
        return Ok(response);
    }

    /// <summary>
    /// 根据刷新令牌换取新的访问令牌。
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshTokenRequestDto request, CancellationToken token)
    {
        var response = await _authService.RefreshAsync(request.RefreshToken, token);
        return Ok(response);
    }

    /// <summary>
    /// 公开注册新用户，便于前台自助创建账号。
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<UserResponseDto>> Register(CreateUserRequestDto request, CancellationToken token)
    {
        var user = await _userService.RegisterAsync(request, token);
        return Created($"/api/user/{user.Id}", user);
    }
}
