using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace Mubai.MonolithicShop.Tests.Integration;

public class SwaggerSecurityTests : IClassFixture<TestUtilities.CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SwaggerSecurityTests(TestUtilities.CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_ShouldExposeBearerSecurityScheme()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        var payload = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, "OpenAPI JSON should be reachable. Response: {0}", payload);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        root.TryGetProperty("components", out var components).Should().BeTrue();
        components.TryGetProperty("securitySchemes", out var securitySchemes).Should().BeTrue();
        securitySchemes.TryGetProperty("Bearer", out var bearer).Should().BeTrue();

        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
        bearer.GetProperty("bearerFormat").GetString().Should().Be("JWT");

        if (root.TryGetProperty("security", out var securityArray) && securityArray.ValueKind == JsonValueKind.Array)
        {
            securityArray.EnumerateArray()
                .Any(entry => entry.TryGetProperty("Bearer", out _))
                .Should().BeTrue("OpenAPI security requirement should reference the Bearer scheme.");
        }
    }
}
