using BillingApp.Domain.Models;
using BillingApp.Infrastructure.Data;

namespace BillingApp.WinFormsUI.Forms;

public class InvoiceListForm : Form
{
    private readonly string _dbPath;
    private readonly InvoiceRepository _invoiceRepository;
    private readonly CustomerRepository _customerRepository;
    private readonly DataGridView _grid;
    private readonly Button _newButton;
    private readonly Button _editButton;
    private readonly Button _refreshButton;
    private readonly TextBox _searchTextBox;

    private List<Invoice> _invoices = new();
    private Dictionary<int, string> _customerLookup = new();

    public InvoiceListForm(string dbPath)
    {
        _dbPath = dbPath;
        var factory = new SqliteConnectionFactory(dbPath);
        _invoiceRepository = new InvoiceRepository(factory);
        _customerRepository = new CustomerRepository(factory);

        Text = "Billing App - Invoices";
        Width = 1000;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        _newButton = new Button { Text = "New Invoice", Width = 120, Height = 32, Left = 10, Top = 10 };
        _editButton = new Button { Text = "Edit Selected", Width = 120, Height = 32, Left = 140, Top = 10 };
        _refreshButton = new Button { Text = "Refresh", Width = 100, Height = 32, Left = 270, Top = 10 };
        _searchTextBox = new TextBox { Left = 700, Top = 14, Width = 270, PlaceholderText = "Search invoice no, customer, status..." };
        _grid = new DataGridView
        {
            Left = 10,
            Top = 55,
            Width = 960,
            Height = 490,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Id", DataPropertyName = "Id", Width = 50 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Invoice No", DataPropertyName = "InvoiceNumber", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Issue Date", DataPropertyName = "IssueDate", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Customer", DataPropertyName = "CustomerName", Width = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Currency", DataPropertyName = "Currency", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total", DataPropertyName = "TotalDisplay", Width = 150 });

        Controls.Add(_newButton);
        Controls.Add(_editButton);
        Controls.Add(_refreshButton);
        Controls.Add(_searchTextBox);
        Controls.Add(_grid);

        Load += (_, _) => RefreshInvoices();
        _refreshButton.Click += (_, _) => RefreshInvoices();
        _newButton.Click += (_, _) => OpenInvoiceForm();
        _editButton.Click += (_, _) => OpenSelectedInvoice();
        _searchTextBox.TextChanged += (_, _) => BindGrid();
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                OpenSelectedInvoice();
        };
    }

    private void RefreshInvoices()
    {
        _invoices = _invoiceRepository.GetAll();
        _customerLookup = _customerRepository.GetAll().ToDictionary(c => c.Id, c => c.Name);
        BindGrid();
    }

    private void BindGrid()
    {
        var query = (_searchTextBox.Text ?? string.Empty).Trim();
        var filtered = _invoices
            .Where(i => string.IsNullOrWhiteSpace(query)
                || i.InvoiceNumber.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (_customerLookup.TryGetValue(i.CustomerId, out var customerName) && customerName.Contains(query, StringComparison.OrdinalIgnoreCase))
                || i.Status.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                IssueDate = i.IssueDate.ToString("yyyy-MM-dd"),
                CustomerName = _customerLookup.TryGetValue(i.CustomerId, out var customerName) ? customerName : $"Customer #{i.CustomerId}",
                i.Status,
                i.Currency,
                TotalDisplay = $"{i.Totals.GrandTotalMinor} {i.Currency}"
            })
            .ToList();

        _grid.DataSource = filtered;
    }

    private void OpenInvoiceForm()
    {
        using var form = new InvoiceForm(_dbPath);
        form.ShowDialog(this);
        RefreshInvoices();
    }

    private void OpenSelectedInvoice()
    {
        if (_grid.CurrentRow?.DataBoundItem is null)
            return;

        var idValue = _grid.CurrentRow.Cells[0].Value?.ToString();
        if (!int.TryParse(idValue, out var invoiceId))
            return;

        using var form = new InvoiceForm(_dbPath, invoiceId);
        form.ShowDialog(this);
        RefreshInvoices();
    }
}
