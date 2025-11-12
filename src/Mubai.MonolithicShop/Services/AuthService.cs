using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Options;
using Mubai.MonolithicShop.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 身份认证服务，负责登录、刷新令牌与 JWT 签发。
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> options,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _jwtOptions = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 校验账号密码并生成新的访问令牌与刷新令牌。
    /// </summary>
    public async Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken token = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
                   ?? throw new InvalidOperationException("账号或密码错误。");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new InvalidOperationException("账号或密码错误。");
        }

        _logger.LogInformation("用户 {Email} 登录成功。", user.Email);

        var response = await IssueTokensAsync(user, token);
        await _unitOfWork.SaveChangesAsync(token);
        return response;
    }

    /// <summary>
    /// 根据刷新令牌换取新的访问令牌。
    /// </summary>
    public async Task<TokenResponseDto> RefreshAsync(string refreshToken, CancellationToken token = default)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, token)
                           ?? throw new InvalidOperationException("刷新令牌不存在。");

        if (!storedToken.IsActive)
        {
            throw new InvalidOperationException("刷新令牌已失效。");
        }

        storedToken.RevokedTime = DateTime.UtcNow;
        _refreshTokenRepository.Update(storedToken);

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString())
                   ?? throw new InvalidOperationException("刷新令牌对应的用户不存在。");

        var response = await IssueTokensAsync(user, token);
        await _unitOfWork.SaveChangesAsync(token);
        return response;
    }

    /// <summary>
    /// 颁发访问令牌与刷新令牌。
    /// </summary>
    private async Task<TokenResponseDto> IssueTokensAsync(ApplicationUser user, CancellationToken token)
    {
        var expiresTime = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var accessToken = GenerateJwt(user, expiresTime);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, token);
        return new TokenResponseDto(accessToken, refreshToken.Token, expiresTime);
    }

    /// <summary>
    /// 构造 JWT 字符串。
    /// </summary>
    private string GenerateJwt(ApplicationUser user, DateTime expiresTime)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            expires: expiresTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
