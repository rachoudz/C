using BillingApp.Infrastructure.Data;
using BillingApp.WinFormsUI.Forms;

namespace BillingApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        Directory.CreateDirectory(appDataPath);
        var dbPath = Path.Combine(appDataPath, "billingapp.db");

        var database = new DatabaseInitializer(dbPath);
        database.Initialize();

        Application.Run(new InvoiceListForm(dbPath));
    }
}
