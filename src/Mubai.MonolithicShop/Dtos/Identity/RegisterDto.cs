using System;
using System.Collections.Generic;
using System.Text;

namespace Mubai.MonolithicShop.Dtos.Identity
{
    public record RegisterDto(string Email, string Name, string Password, string? PhoneNumber);
}
