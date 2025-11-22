using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Filters;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Options;
using Mubai.MonolithicShop.Repositories;
using Mubai.MonolithicShop.Services;
using Mubai.Snowflake;
using Mubai.UnitOfWork.Abstractions;
using Mubai.UnitOfWork.EntityFrameworkCore;

namespace Mubai.MonolithicShop.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName));
        services.ConfigureOptions<ConfigureDatabaseOptions>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.ConfigureOptions<ConfigureJwtBearerOptions>();

        services.AddDbContext<ShopDbContext>((serviceProvider, options) =>
        {
            var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            if (string.Equals(dbOptions.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                var databaseName = dbOptions.ConnectionString ?? $"mubai-tests-{Guid.NewGuid():N}";
                options.UseInMemoryDatabase(databaseName);
                return;
            }

            if (string.Equals(dbOptions.Provider, DatabaseOptions.SqliteProvider, StringComparison.OrdinalIgnoreCase))
            {
                var sqliteConn = dbOptions.SqliteConnectionString ?? DatabaseOptions.DefaultSqliteConnection;
                options.UseSqlite(sqliteConn);
                return;
            }

            var connectionString = dbOptions.ConnectionString
                                   ?? configuration.GetConnectionString("Default")
                                   ?? DatabaseOptions.DefaultMySqlConnection;
            options.UseSqlServer(connectionString);
        });

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<ShopDbContext>()
            .AddDefaultTokenProviders();

        services.AddSnowflakeIdGenerator(options => options.WorkerId = 1);

        services.AddScoped<IUnitOfWork<ShopDbContext>, EfUnitOfWork<ShopDbContext>>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ApiExceptionFilter>();
        });

        services.AddOpenApi(options =>
        {
            // Specify the OpenAPI version to use
            options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;

            // Add JWT bearer security scheme so Swagger UI supports Authorize
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                var bearerScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "JWT Bearer token"
                };
                document.Components.SecuritySchemes["Bearer"] = bearerScheme;

                var paths = document.Paths;
                if (paths is not null)
                {
                    foreach (var path in paths.Values)
                    {
                        foreach (var operation in path.Operations.Values)
                        {
                            operation.Security ??= new List<OpenApiSecurityRequirement>();
                            operation.Security.Add(new OpenApiSecurityRequirement
                            {
                                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                            });
                        }
                    }
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
