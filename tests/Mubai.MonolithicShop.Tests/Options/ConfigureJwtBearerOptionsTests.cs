using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Mubai.MonolithicShop.Options;

namespace Mubai.MonolithicShop.Tests.Configuration;

public class ConfigureJwtBearerOptionsTests
{
    [Fact]
    public void Configure_ShouldPopulateTokenValidationParameters()
    {
        var sourceOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "issuer",
            Audience = "aud",
            SigningKey = "super-secret-signing-key-123456"
        });
        var configure = new ConfigureJwtBearerOptions(sourceOptions);
        var jwtOptions = new JwtBearerOptions();

        configure.Configure(jwtOptions);

        jwtOptions.TokenValidationParameters.Should().NotBeNull();
        jwtOptions.TokenValidationParameters.ValidIssuer.Should().Be("issuer");
        jwtOptions.TokenValidationParameters.ValidAudience.Should().Be("aud");
        jwtOptions.TokenValidationParameters.IssuerSigningKey.Should().NotBeNull();
    }
}
