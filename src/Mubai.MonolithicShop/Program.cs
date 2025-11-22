using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mubai.MonolithicShop.Extensions;
using Mubai.MonolithicShop.Options;

namespace Mubai.MonolithicShop;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureEnvironment(builder);

        builder.Services
            .AddInfrastructure(builder.Configuration)
            .AddApplication()
            .AddApi();

        var app = builder.Build();
        app.ApplyMigrations();

        ConfigureMiddleware(app);

        app.Run();
    }

    private static void ConfigureEnvironment(WebApplicationBuilder builder)
    {
        var environmentFromConfig = builder.Configuration.GetValue<string>("Environment");
        if (!string.IsNullOrWhiteSpace(environmentFromConfig))
        {
            builder.Host.UseEnvironment(environmentFromConfig);
        }
    }
    private static void ConfigureMiddleware(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Mubai.MonolithicShop (OpenAPI 3.1)");
            });
        }
        
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
