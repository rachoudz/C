namespace BillingApp.Application.Services;

public static class MoneyHelper
{
    public static long ToMinor(decimal amount)
    {
        return (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal FromMinor(long amountMinor)
    {
        return amountMinor;
    }
}
