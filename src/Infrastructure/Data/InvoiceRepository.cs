using BillingApp.Domain.Models;
using Microsoft.Data.Sqlite;

namespace BillingApp.Infrastructure.Data;

public class InvoiceRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public InvoiceRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<Invoice> GetAll()
    {
        using var connection = _connectionFactory.Create();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, invoice_number, customer_id, issue_date, due_date, status, currency, notes, date_in, date_out, storage_days, subtotal_minor, tax_total_minor, grand_total_minor
FROM invoices ORDER BY issue_date DESC, id DESC";

        using var reader = command.ExecuteReader();
        var invoices = new List<Invoice>();
        while (reader.Read())
        {
            invoices.Add(MapInvoiceSummary(reader));
        }

        return invoices;
    }

    public Invoice? GetById(int invoiceId)
    {
        using var connection = _connectionFactory.Create();
        connection.Open();

        Invoice? invoice;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"SELECT id, invoice_number, customer_id, issue_date, due_date, status, currency, notes, date_in, date_out, storage_days, subtotal_minor, tax_total_minor, grand_total_minor
FROM invoices WHERE id = $id";
            command.Parameters.AddWithValue("$id", invoiceId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return null;

            invoice = MapInvoiceSummary(reader);
        }

        using (var itemsCommand = connection.CreateCommand())
        {
            itemsCommand.CommandText = @"SELECT id, invoice_id, line_no, product_id, item_type, description, quantity, unit_name, unit_price_minor, tax_rate, line_subtotal_minor, line_tax_minor, line_total_minor, storage_start_date, storage_end_date, storage_days, calculation_note
FROM invoice_items
WHERE invoice_id = $invoiceId
ORDER BY line_no";
            itemsCommand.Parameters.AddWithValue("$invoiceId", invoiceId);

            using var reader = itemsCommand.ExecuteReader();
            while (reader.Read())
            {
                invoice.Items.Add(new InvoiceItem
                {
                    Id = reader.GetInt32(0),
                    InvoiceId = reader.GetInt32(1),
                    LineNo = reader.GetInt32(2),
                    ProductId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    PricingRuleType = reader.GetString(4) == "storage_daily" ? PricingRuleType.StorageDaily : PricingRuleType.FixedPrice,
                    Description = reader.GetString(5),
                    Quantity = reader.GetDecimal(6),
                    UnitName = reader.GetString(7),
                    UnitPriceMinor = reader.GetInt64(8),
                    TaxRate = reader.GetDecimal(9),
                    LineSubtotalMinor = reader.GetInt64(10),
                    LineTaxMinor = reader.GetInt64(11),
                    LineTotalMinor = reader.GetInt64(12),
                    StorageStartDate = reader.IsDBNull(13) ? null : DateTime.Parse(reader.GetString(13)),
                    StorageEndDate = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14)),
                    StorageDays = reader.IsDBNull(15) ? 0 : reader.GetInt32(15),
                    CalculationNote = reader.IsDBNull(16) ? null : reader.GetString(16)
                });
            }
        }

        return invoice;
    }

    public int Save(Invoice invoice)
    {
        using var connection = _connectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        int invoiceId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            if (invoice.Id == 0)
            {
                command.CommandText = @"INSERT INTO invoices (invoice_number, customer_id, issue_date, due_date, status, currency, notes, date_in, date_out, storage_days, subtotal_minor, tax_total_minor, grand_total_minor)
VALUES ($invoiceNumber, $customerId, $issueDate, $dueDate, $status, $currency, $notes, $dateIn, $dateOut, $storageDays, $subtotal, $tax, $grand);
SELECT last_insert_rowid();";
            }
            else
            {
                command.CommandText = @"UPDATE invoices SET invoice_number=$invoiceNumber, customer_id=$customerId, issue_date=$issueDate, due_date=$dueDate, status=$status, currency=$currency, notes=$notes, date_in=$dateIn, date_out=$dateOut, storage_days=$storageDays, subtotal_minor=$subtotal, tax_total_minor=$tax, grand_total_minor=$grand, updated_at=CURRENT_TIMESTAMP WHERE id=$id;
SELECT $id;";
                command.Parameters.AddWithValue("$id", invoice.Id);
            }

            command.Parameters.AddWithValue("$invoiceNumber", invoice.InvoiceNumber);
            command.Parameters.AddWithValue("$customerId", invoice.CustomerId);
            command.Parameters.AddWithValue("$issueDate", invoice.IssueDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("$dueDate", (object?)invoice.DueDate?.ToString("yyyy-MM-dd") ?? DBNull.Value);
            command.Parameters.AddWithValue("$status", invoice.Status);
            command.Parameters.AddWithValue("$currency", invoice.Currency);
            command.Parameters.AddWithValue("$notes", (object?)invoice.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("$dateIn", (object?)invoice.DateIn?.ToString("yyyy-MM-dd") ?? DBNull.Value);
            command.Parameters.AddWithValue("$dateOut", (object?)invoice.DateOut?.ToString("yyyy-MM-dd") ?? DBNull.Value);
            command.Parameters.AddWithValue("$storageDays", invoice.StorageDays);
            command.Parameters.AddWithValue("$subtotal", invoice.Totals.SubtotalMinor);
            command.Parameters.AddWithValue("$tax", invoice.Totals.TaxTotalMinor);
            command.Parameters.AddWithValue("$grand", invoice.Totals.GrandTotalMinor);

            invoiceId = Convert.ToInt32(command.ExecuteScalar());
        }

        using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM invoice_items WHERE invoice_id = $invoiceId";
            deleteCommand.Parameters.AddWithValue("$invoiceId", invoiceId);
            deleteCommand.ExecuteNonQuery();
        }

        foreach (var item in invoice.Items.OrderBy(i => i.LineNo))
        {
            using var itemCommand = connection.CreateCommand();
            itemCommand.Transaction = transaction;
            itemCommand.CommandText = @"INSERT INTO invoice_items (invoice_id, line_no, product_id, item_type, description, quantity, unit_name, unit_price_minor, tax_rate, line_subtotal_minor, line_tax_minor, line_total_minor, storage_start_date, storage_end_date, storage_days, calculation_note)
VALUES ($invoiceId, $lineNo, $productId, $itemType, $description, $quantity, $unitName, $unitPrice, $taxRate, $lineSubtotal, $lineTax, $lineTotal, $storageStart, $storageEnd, $storageDays, $calculationNote)";
            itemCommand.Parameters.AddWithValue("$invoiceId", invoiceId);
            itemCommand.Parameters.AddWithValue("$lineNo", item.LineNo);
            itemCommand.Parameters.AddWithValue("$productId", (object?)item.ProductId ?? DBNull.Value);
            itemCommand.Parameters.AddWithValue("$itemType", item.PricingRuleType == PricingRuleType.StorageDaily ? "storage_daily" : "fixed");
            itemCommand.Parameters.AddWithValue("$description", item.Description);
            itemCommand.Parameters.AddWithValue("$quantity", item.Quantity);
            itemCommand.Parameters.AddWithValue("$unitName", item.UnitName);
            itemCommand.Parameters.AddWithValue("$unitPrice", item.UnitPriceMinor);
            itemCommand.Parameters.AddWithValue("$taxRate", item.TaxRate);
            itemCommand.Parameters.AddWithValue("$lineSubtotal", item.LineSubtotalMinor);
            itemCommand.Parameters.AddWithValue("$lineTax", item.LineTaxMinor);
            itemCommand.Parameters.AddWithValue("$lineTotal", item.LineTotalMinor);
            itemCommand.Parameters.AddWithValue("$storageStart", (object?)item.StorageStartDate?.ToString("yyyy-MM-dd") ?? DBNull.Value);
            itemCommand.Parameters.AddWithValue("$storageEnd", (object?)item.StorageEndDate?.ToString("yyyy-MM-dd") ?? DBNull.Value);
            itemCommand.Parameters.AddWithValue("$storageDays", item.StorageDays == 0 ? DBNull.Value : item.StorageDays);
            itemCommand.Parameters.AddWithValue("$calculationNote", (object?)item.CalculationNote ?? DBNull.Value);
            itemCommand.ExecuteNonQuery();
        }

        transaction.Commit();
        return invoiceId;
    }

    private static Invoice MapInvoiceSummary(SqliteDataReader reader)
    {
        return new Invoice
        {
            Id = reader.GetInt32(0),
            InvoiceNumber = reader.GetString(1),
            CustomerId = reader.GetInt32(2),
            IssueDate = DateTime.Parse(reader.GetString(3)),
            DueDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
            Status = reader.GetString(5),
            Currency = reader.GetString(6),
            Notes = reader.IsDBNull(7) ? null : reader.GetString(7),
            DateIn = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
            DateOut = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9)),
            StorageDays = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
            Totals = new InvoiceTotals
            {
                SubtotalMinor = reader.GetInt64(11),
                TaxTotalMinor = reader.GetInt64(12),
                GrandTotalMinor = reader.GetInt64(13)
            }
        };
    }
}
