using System.Text;
using BillingApp.Domain.Models;

namespace BillingApp.Application.Services;

public class InvoicePrintDocumentBuilder
{
    public string BuildPlainText(Invoice invoice, string customerName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("BILLING APP");
        sb.AppendLine($"Invoice: {invoice.InvoiceNumber}");
        sb.AppendLine($"Customer: {customerName}");
        sb.AppendLine($"Issue Date: {invoice.IssueDate:yyyy-MM-dd}");
        if (invoice.DateIn.HasValue)
            sb.AppendLine($"Date In: {invoice.DateIn:yyyy-MM-dd}");
        if (invoice.DateOut.HasValue)
            sb.AppendLine($"Date Out: {invoice.DateOut:yyyy-MM-dd}");

        sb.AppendLine(new string('-', 60));
        foreach (var item in invoice.Items.OrderBy(i => i.LineNo))
        {
            sb.AppendLine($"{item.LineNo}. {item.Description}");
            sb.AppendLine($"   Qty: {item.Quantity} {item.UnitName} | Unit: {item.UnitPriceMinor} | Tax: {item.TaxRate}% | Total: {item.LineTotalMinor}");
            if (item.PricingRuleType == PricingRuleType.StorageDaily)
                sb.AppendLine($"   Storage: {item.StorageStartDate:yyyy-MM-dd} -> {item.StorageEndDate:yyyy-MM-dd} | Days: {item.StorageDays}");
        }

        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Subtotal: {invoice.Totals.SubtotalMinor} {invoice.Currency}");
        sb.AppendLine($"Tax: {invoice.Totals.TaxTotalMinor} {invoice.Currency}");
        sb.AppendLine($"Grand Total: {invoice.Totals.GrandTotalMinor} {invoice.Currency}");

        return sb.ToString();
    }
}
