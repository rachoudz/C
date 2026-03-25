namespace BillingApp.Domain.Models;

public class InvoiceTotals
{
    public long SubtotalMinor { get; set; }
    public long TaxTotalMinor { get; set; }
    public long GrandTotalMinor { get; set; }
}
