namespace Mubai.MonolithicShop.Entities;

/// <summary>
/// 刷新令牌
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// 刷新令牌主键。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属用户主键。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 关联用户信息。
    /// </summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// 刷新令牌字符串。
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间。
    /// </summary>
    public DateTime ExpiresTime { get; set; }

    /// <summary>
    /// 撤销时间。
    /// </summary>
    public DateTime? RevokedTime { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否仍然有效。
    /// </summary>
    public bool IsActive => RevokedTime == null && DateTime.UtcNow < ExpiresTime;
}
