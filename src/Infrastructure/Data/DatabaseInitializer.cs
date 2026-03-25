using Microsoft.Data.Sqlite;

namespace BillingApp.Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src", "Infrastructure", "Data", "schema.sql");
        if (!File.Exists(schemaPath))
        {
            schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "Infrastructure", "Data", "schema.sql");
        }

        var sql = File.ReadAllText(schemaPath);
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();

        SeedProducts(connection);
    }

    private static void SeedProducts(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
INSERT INTO products (sku, name, description, item_type, unit_name, default_unit_price_minor, tax_rate)
SELECT 'DS6', 'Magasinage et gardiennage', 'Storage charge billed per day', 'storage_daily', 'day', 10300, 19
WHERE NOT EXISTS (SELECT 1 FROM products WHERE sku = 'DS6');

INSERT INTO products (sku, name, description, item_type, unit_name, default_unit_price_minor, tax_rate)
SELECT 'FIXED-001', 'Ouverture dossier', 'Fixed service charge', 'fixed', 'pcs', 5000, 19
WHERE NOT EXISTS (SELECT 1 FROM products WHERE sku = 'FIXED-001');
";
        cmd.ExecuteNonQuery();
    }
}
