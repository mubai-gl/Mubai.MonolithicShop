using Mubai.MonolithicShop.Dtos;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 身份认证服务契约。
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 使用邮箱与密码登录。
    /// </summary>
    Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 根据刷新令牌获取新的访问令牌。
    /// </summary>
    Task<TokenResponseDto> RefreshAsync(string refreshToken, CancellationToken token = default);
}
