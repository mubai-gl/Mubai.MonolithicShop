using Mubai.MonolithicShop.Dtos;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 用户服务契约。
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 注册新用户。
    /// </summary>
    Task<UserResponseDto> RegisterAsync(CreateUserRequestDto request, CancellationToken token = default);

    /// <summary>
    /// 根据主键获取用户。
    /// </summary>
    Task<UserResponseDto?> GetAsync(Guid id, CancellationToken token = default);

    /// <summary>
    /// 获取全部用户列表。
    /// </summary>
    Task<IReadOnlyCollection<UserResponseDto>> GetAllAsync(CancellationToken token = default);
}
