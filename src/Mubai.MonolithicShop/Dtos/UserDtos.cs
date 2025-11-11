namespace Mubai.MonolithicShop.Dtos;

public record UserResponseDto(Guid Id, string Email, string Name, string? PhoneNumber);

public record CreateUserRequestDto(string Email, string Name, string Password, string? PhoneNumber);

public record UpdateUserRequestDto(Guid Id, string Name, string? PhoneNumber);
