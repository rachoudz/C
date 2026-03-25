namespace BillingApp.Application.Services;

public class CalculationResult
{
    public long NetAmountMinor { get; set; }
    public long TaxAmountMinor { get; set; }
    public long GrossAmountMinor { get; set; }
    public int ComputedDays { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
