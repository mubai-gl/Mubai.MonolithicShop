namespace Mubai.MonolithicShop.Dtos;

public record LoginRequestDto(string Email, string Password);

public record RegisterRequestDto(string Email, string Name, string Password, string? PhoneNumber);

public record TokenResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresTime);

public record RefreshTokenRequestDto(string RefreshToken);
