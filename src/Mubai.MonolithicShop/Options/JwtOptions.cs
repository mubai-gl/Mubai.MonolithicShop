namespace Mubai.MonolithicShop.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "Mubai.MonolithicShop";
    public string Audience { get; set; } = "Mubai.MonolithicShop";
    public string SigningKey { get; set; } = "ChangeMeSuperSecretKey123!";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
}
