namespace BillingApp.Domain.Models;

public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int LineNo { get; set; }
    public int? ProductId { get; set; }
    public PricingRuleType PricingRuleType { get; set; } = PricingRuleType.FixedPrice;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public string UnitName { get; set; } = "pcs";
    public long UnitPriceMinor { get; set; }
    public decimal TaxRate { get; set; }

    public DateTime? StorageStartDate { get; set; }
    public DateTime? StorageEndDate { get; set; }
    public int StorageDays { get; set; }

    public long LineSubtotalMinor { get; set; }
    public long LineTaxMinor { get; set; }
    public long LineTotalMinor { get; set; }
    public string? CalculationNote { get; set; }
}
