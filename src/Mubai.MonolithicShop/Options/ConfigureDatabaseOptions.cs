using Microsoft.Extensions.Options;

namespace Mubai.MonolithicShop.Options;

public sealed class ConfigureDatabaseOptions : IConfigureOptions<DatabaseOptions>
{
    private readonly IConfiguration _configuration;

    public ConfigureDatabaseOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(DatabaseOptions options)
    {
        var databaseSection = _configuration.GetSection(DatabaseOptions.SectionName);

        options.Provider = databaseSection["Provider"]
                           ?? _configuration.GetValue<string>("DatabaseProvider")
                           ?? options.Provider
                           ?? DatabaseOptions.DefaultProvider;

        options.ConnectionString = databaseSection["ConnectionString"]
                                   ?? _configuration.GetConnectionString("Default")
                                   ?? options.ConnectionString;

        options.SqliteConnectionString = databaseSection["SqliteConnectionString"]
                                         ?? _configuration.GetConnectionString("Sqlite")
                                         ?? options.SqliteConnectionString
                                         ?? DatabaseOptions.DefaultSqliteConnection;
    }
}
