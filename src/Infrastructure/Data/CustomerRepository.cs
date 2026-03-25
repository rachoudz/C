using BillingApp.Domain.Models;
using Microsoft.Data.Sqlite;

namespace BillingApp.Infrastructure.Data;

public class CustomerRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public CustomerRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<Customer> GetAll()
    {
        using var connection = _connectionFactory.Create();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, code, name, phone, email, address_line1, city, country, tax_id, notes, is_active FROM customers WHERE is_active = 1 ORDER BY name";

        using var reader = command.ExecuteReader();
        var customers = new List<Customer>();
        while (reader.Read())
        {
            customers.Add(new Customer
            {
                Id = reader.GetInt32(0),
                Code = reader.IsDBNull(1) ? null : reader.GetString(1),
                Name = reader.GetString(2),
                Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                AddressLine1 = reader.IsDBNull(5) ? null : reader.GetString(5),
                City = reader.IsDBNull(6) ? null : reader.GetString(6),
                Country = reader.IsDBNull(7) ? null : reader.GetString(7),
                TaxId = reader.IsDBNull(8) ? null : reader.GetString(8),
                Notes = reader.IsDBNull(9) ? null : reader.GetString(9),
                IsActive = reader.GetInt64(10) == 1
            });
        }

        return customers;
    }

    public int Save(Customer customer)
    {
        using var connection = _connectionFactory.Create();
        connection.Open();

        using var command = connection.CreateCommand();
        if (customer.Id == 0)
        {
            command.CommandText = @"INSERT INTO customers (code, name, phone, email, address_line1, city, country, tax_id, notes, is_active)
VALUES ($code, $name, $phone, $email, $address, $city, $country, $taxId, $notes, $isActive);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"UPDATE customers SET code=$code, name=$name, phone=$phone, email=$email, address_line1=$address, city=$city, country=$country, tax_id=$taxId, notes=$notes, is_active=$isActive, updated_at=CURRENT_TIMESTAMP WHERE id=$id;
SELECT $id;";
            command.Parameters.AddWithValue("$id", customer.Id);
        }

        command.Parameters.AddWithValue("$code", (object?)customer.Code ?? DBNull.Value);
        command.Parameters.AddWithValue("$name", customer.Name);
        command.Parameters.AddWithValue("$phone", (object?)customer.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("$email", (object?)customer.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("$address", (object?)customer.AddressLine1 ?? DBNull.Value);
        command.Parameters.AddWithValue("$city", (object?)customer.City ?? DBNull.Value);
        command.Parameters.AddWithValue("$country", (object?)customer.Country ?? DBNull.Value);
        command.Parameters.AddWithValue("$taxId", (object?)customer.TaxId ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)customer.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$isActive", customer.IsActive ? 1 : 0);

        return Convert.ToInt32(command.ExecuteScalar());
    }
}
