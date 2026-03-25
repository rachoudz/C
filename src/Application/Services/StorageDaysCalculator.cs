namespace BillingApp.Application.Services;

public static class StorageDaysCalculator
{
    public static int ComputeBillableDays(
        DateTime startDate,
        DateTime endDate,
        bool inclusiveEndDate = true,
        int minimumBillableDays = 1,
        int freeDays = 0)
    {
        var start = startDate.Date;
        var end = endDate.Date;

        if (end < start)
            throw new ArgumentException("Storage end date cannot be before start date.");

        int rawDays = inclusiveEndDate
            ? (end - start).Days + 1
            : (end - start).Days;

        int afterFreeDays = Math.Max(0, rawDays - freeDays);
        return Math.Max(afterFreeDays, minimumBillableDays);
    }
}
