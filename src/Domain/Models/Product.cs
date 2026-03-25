namespace BillingApp.Domain.Models;

public class Product
{
    public int Id { get; set; }
    public string? Sku { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PricingRuleType PricingRuleType { get; set; } = PricingRuleType.FixedPrice;
    public string UnitName { get; set; } = "pcs";
    public long DefaultUnitPriceMinor { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsActive { get; set; } = true;
}
