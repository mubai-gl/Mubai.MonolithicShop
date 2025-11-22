using Mubai.MonolithicShop.Dtos.Identity;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 用户服务契约，定义注册与查询能力。
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 注册新用户。
    /// </summary>
    Task RegisterAsync(CreateUserDto request, CancellationToken token = default);

    /// <summary>
    /// 按照用户标识获取单个用户。
    /// </summary>
    Task<UserResponseDto?> GetAsync(Guid id, CancellationToken token = default);

    /// <summary>
    /// 获取全部用户列表。
    /// </summary>
    Task<IReadOnlyCollection<UserResponseDto>> GetAllAsync(CancellationToken token = default);
}
