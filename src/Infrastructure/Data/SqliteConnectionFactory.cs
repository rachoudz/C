using Microsoft.Data.Sqlite;

namespace BillingApp.Infrastructure.Data;

public class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public SqliteConnection Create()
    {
        return new SqliteConnection(_connectionString);
    }
}
