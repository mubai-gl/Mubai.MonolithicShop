using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Filters;
using Mubai.MonolithicShop.Options;
using Mubai.MonolithicShop.Repositories;
using Mubai.MonolithicShop.Services;
using Mubai.Snowflake;
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
            //if (string.Equals(dbOptions.Provider, DatabaseOptions.SqliteProvider, StringComparison.OrdinalIgnoreCase))
            //{
            //    var sqliteConn = dbOptions.SqliteConnectionString ?? DatabaseOptions.DefaultSqliteConnection;
            //    options.UseSqlite(sqliteConn);
            //    return;
            //}

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

        services.AddScoped<IEfUnitOfWork<ShopDbContext>, EfUnitOfWork<ShopDbContext>>();
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
            // 给整个文档加 Bearer 安全定义 + 全局 requirement
            options.AddDocumentTransformer<BearerSecurityTransformer>();
        });

        return services;
    }

    internal sealed class BearerSecurityTransformer(
    IAuthenticationSchemeProvider schemeProvider) : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var schemes = await schemeProvider.GetAllSchemesAsync();
            if (!schemes.Any(s => s.Name == "Bearer"))
            {
                return;
            }

            // 顶层 Components.SecuritySchemes 里加 Bearer
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "在此处输入 Bearer Token，格式为 Bearer {token}。"
            };

            // 所有 operation 加上 SecurityRequirement（相当于全局“加锁”）
            foreach (var operation in document.Paths.Values.SelectMany(p => p.Operations.Values))
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            }
        }
    }
}
