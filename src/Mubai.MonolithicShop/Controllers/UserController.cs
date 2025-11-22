using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mubai.MonolithicShop.Dtos.Identity;
using Mubai.MonolithicShop.Services;

namespace Mubai.MonolithicShop.Controllers;

/// <summary>
/// 用户管理接口，支持注册与查询。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// 注册新用户。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Register(CreateUserDto request, CancellationToken token)
    {
        await _userService.RegisterAsync(request, token);
        return Ok();
    }

    /// <summary>
    /// 根据主键获取单个用户信息。
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponseDto>> Get(Guid id, CancellationToken token)
    {
        var user = await _userService.GetAsync(id, token);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// 获取全部用户列表。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> List(CancellationToken token)
    {
        var users = await _userService.GetAllAsync(token);
        return Ok(users);
    }
}
