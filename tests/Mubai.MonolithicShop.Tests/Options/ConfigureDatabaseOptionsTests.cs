using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Mubai.MonolithicShop.Options;

namespace Mubai.MonolithicShop.Tests.Configuration;

public class ConfigureDatabaseOptionsTests
{
    [Fact]
    public void Configure_ShouldApplyValuesFromConfiguration()
    {
        var data = new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:ConnectionString"] = "Data Source=tests.db",
            ["Database:SqliteConnectionString"] = "Data Source=tests.db",
            ["ConnectionStrings:Default"] = "Server=dummy;"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
        var options = new DatabaseOptions();
        var configure = new ConfigureDatabaseOptions(configuration);

        configure.Configure(options);

        options.Provider.Should().Be("Sqlite");
        options.ConnectionString.Should().Be("Data Source=tests.db");
        options.SqliteConnectionString.Should().Be("Data Source=tests.db");
    }
}
