using BillingApp.Domain.Models;

namespace BillingApp.Infrastructure.Data;

public class ProductRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ProductRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<Product> GetAllActive()
    {
        using var connection = _connectionFactory.Create();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, sku, name, description, item_type, unit_name, default_unit_price_minor, tax_rate, is_active FROM products WHERE is_active = 1 ORDER BY name";

        using var reader = command.ExecuteReader();
        var items = new List<Product>();
        while (reader.Read())
        {
            items.Add(new Product
            {
                Id = reader.GetInt32(0),
                Sku = reader.IsDBNull(1) ? null : reader.GetString(1),
                Name = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                PricingRuleType = reader.GetString(4) == "storage_daily" ? PricingRuleType.StorageDaily : PricingRuleType.FixedPrice,
                UnitName = reader.GetString(5),
                DefaultUnitPriceMinor = reader.GetInt64(6),
                TaxRate = reader.GetDecimal(7),
                IsActive = reader.GetInt64(8) == 1
            });
        }

        return items;
    }
}
