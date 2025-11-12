using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Filters;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Options;
using Mubai.MonolithicShop.Repositories;
using Mubai.MonolithicShop.Services;
using Mubai.Snowflake;

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
            if (string.Equals(dbOptions.Provider, DatabaseOptions.SqliteProvider, StringComparison.OrdinalIgnoreCase))
            {
                var sqliteConn = dbOptions.SqliteConnectionString ?? DatabaseOptions.DefaultSqliteConnection;
                options.UseSqlite(sqliteConn);
                return;
            }

            var connectionString = dbOptions.ConnectionString
                                   ?? configuration.GetConnectionString("Default")
                                   ?? DatabaseOptions.DefaultMySqlConnection;
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            options.UseMySql(connectionString, serverVersion);
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

        services.AddScoped<IUnitOfWork, UnitOfWork>();
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

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Mubai.MonolithicShop API",
                Version = "v1"
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "在此处输入 Bearer Token，格式为 Bearer {token}。",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    securityScheme, Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
