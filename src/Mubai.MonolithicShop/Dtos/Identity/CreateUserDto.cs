namespace Mubai.MonolithicShop.Dtos.Identity
{
    public record CreateUserDto(string Email, string Name, string Password, string? PhoneNumber);
}
