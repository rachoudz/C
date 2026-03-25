using BillingApp.Domain.Models;

namespace BillingApp.Application.Services;

public class InvoiceCalculator
{
    public CalculationResult CalculateLine(
        InvoiceItem item,
        bool inclusiveEndDate = true,
        int minimumBillableDays = 1,
        int freeDays = 0)
    {
        if (item.Quantity < 0)
            throw new ArgumentException("Quantity cannot be negative.");

        decimal net;
        int computedDays = 0;
        string explanation;

        switch (item.PricingRuleType)
        {
            case PricingRuleType.StorageDaily:
                if (!item.StorageStartDate.HasValue || !item.StorageEndDate.HasValue)
                    throw new ArgumentException("Storage dates are required for storage pricing.");

                computedDays = StorageDaysCalculator.ComputeBillableDays(
                    item.StorageStartDate.Value,
                    item.StorageEndDate.Value,
                    inclusiveEndDate,
                    minimumBillableDays,
                    freeDays);

                item.StorageDays = computedDays;
                net = computedDays * item.UnitPriceMinor * (item.Quantity <= 0 ? 1 : item.Quantity);
                explanation = $"{computedDays} day(s) × {item.UnitPriceMinor} × qty {(item.Quantity <= 0 ? 1 : item.Quantity)}";
                break;

            case PricingRuleType.FixedPrice:
            default:
                net = item.UnitPriceMinor * item.Quantity;
                explanation = $"{item.Quantity} × {item.UnitPriceMinor}";
                break;
        }

        var netMinor = MoneyHelper.ToMinor(net);
        var taxMinor = MoneyHelper.ToMinor(net * item.TaxRate / 100m);
        var grossMinor = netMinor + taxMinor;

        item.LineSubtotalMinor = netMinor;
        item.LineTaxMinor = taxMinor;
        item.LineTotalMinor = grossMinor;
        item.CalculationNote = explanation;

        return new CalculationResult
        {
            NetAmountMinor = netMinor,
            TaxAmountMinor = taxMinor,
            GrossAmountMinor = grossMinor,
            ComputedDays = computedDays,
            Explanation = explanation
        };
    }

    public InvoiceTotals CalculateInvoiceTotals(
        Invoice invoice,
        bool inclusiveEndDate = true,
        int minimumBillableDays = 1,
        int freeDays = 0)
    {
        long subtotal = 0;
        long tax = 0;
        long gross = 0;

        foreach (var item in invoice.Items)
        {
            var line = CalculateLine(item, inclusiveEndDate, minimumBillableDays, freeDays);
            subtotal += line.NetAmountMinor;
            tax += line.TaxAmountMinor;
            gross += line.GrossAmountMinor;
        }

        invoice.Totals = new InvoiceTotals
        {
            SubtotalMinor = subtotal,
            TaxTotalMinor = tax,
            GrandTotalMinor = gross
        };

        return invoice.Totals;
    }
}
