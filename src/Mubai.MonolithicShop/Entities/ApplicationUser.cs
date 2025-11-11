using Microsoft.AspNetCore.Identity;

namespace Mubai.MonolithicShop.Entities;

/// <summary>
/// 用户
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// 名称，默认与邮箱一致。
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最近更新时间。
    /// </summary>
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
}
