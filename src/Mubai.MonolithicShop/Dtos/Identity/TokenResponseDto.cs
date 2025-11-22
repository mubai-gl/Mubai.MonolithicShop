namespace Mubai.MonolithicShop.Dtos.Identity
{
    public record TokenResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresTime);
}
