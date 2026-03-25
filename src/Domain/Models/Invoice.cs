namespace BillingApp.Domain.Models;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "draft";
    public string Currency { get; set; } = "DZD";
    public string? Notes { get; set; }

    public DateTime? DateIn { get; set; }
    public DateTime? DateOut { get; set; }
    public int StorageDays { get; set; }

    public List<InvoiceItem> Items { get; set; } = new();
    public InvoiceTotals Totals { get; set; } = new();
}
