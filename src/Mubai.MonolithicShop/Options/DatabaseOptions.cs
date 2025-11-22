namespace Mubai.MonolithicShop.Options;

public class DatabaseOptions
{
    public const string SectionName = "Database";
    public const string SqliteProvider = "Sqlite";
    public const string DefaultProvider = "MySql";
    public const string DefaultSqliteConnection = "Data Source=app.db";
    public const string DefaultMySqlConnection = "server=localhost;port=3306;database=mubai_shop;user=root;password=ChangeMe123!;TreatTinyAsBoolean=false;";

    public string Provider { get; set; } = DefaultProvider;
    public string? ConnectionString { get; set; }
    public string? SqliteConnectionString { get; set; }
}
