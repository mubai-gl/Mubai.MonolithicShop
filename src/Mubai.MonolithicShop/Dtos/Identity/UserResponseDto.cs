namespace Mubai.MonolithicShop.Dtos.Identity
{
    public record UserResponseDto(Guid Id, string Email, string Name, string? PhoneNumber);
}
