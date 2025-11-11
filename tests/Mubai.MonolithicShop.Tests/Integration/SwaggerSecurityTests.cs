using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

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
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var payload = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Swagger JSON should be reachable. Response: {0}", payload);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var securitySchemes = root.GetProperty("components").GetProperty("securitySchemes");
        var bearer = securitySchemes.GetProperty("Bearer");

        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
        bearer.GetProperty("bearerFormat").GetString().Should().Be("JWT");

        var securityArray = root.GetProperty("security");
        securityArray.ValueKind.Should().Be(JsonValueKind.Array);
        securityArray.EnumerateArray()
            .Any(entry => entry.TryGetProperty("Bearer", out _))
            .Should().BeTrue("OpenAPI security requirement should reference the Bearer scheme.");
    }
}
