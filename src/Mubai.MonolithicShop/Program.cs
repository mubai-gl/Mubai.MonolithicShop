using Mubai.MonolithicShop.Extensions;

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
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Mubai.MonolithicShop API");
                options.RoutePrefix = string.Empty;
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
