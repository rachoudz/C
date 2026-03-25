using BillingApp.Application.Services;
using BillingApp.Domain.Models;
using BillingApp.Infrastructure.Data;

namespace BillingApp.WinFormsUI.Forms;

public class InvoiceForm : Form
{
    private readonly CustomerRepository _customerRepository;
    private readonly ProductRepository _productRepository;
    private readonly InvoiceRepository _invoiceRepository;
    private readonly InvoiceCalculator _calculator = new();
    private readonly int? _invoiceId;

    private readonly TextBox _invoiceNumberTextBox = new() { Left = 20, Top = 20, Width = 180 };
    private readonly DateTimePicker _issueDatePicker = new() { Left = 220, Top = 20, Width = 150 };
    private readonly ComboBox _customerComboBox = new() { Left = 390, Top = 20, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _dateInPicker = new() { Left = 20, Top = 70, Width = 150 };
    private readonly DateTimePicker _dateOutPicker = new() { Left = 190, Top = 70, Width = 150 };
    private readonly NumericUpDown _dailyRateInput = new() { Left = 360, Top = 70, Width = 120, DecimalPlaces = 0, Maximum = 100000000 };
    private readonly Label _storageDaysLabel = new() { Left = 500, Top = 74, Width = 180, Text = "Storage Days: 0" };
    private readonly DataGridView _itemsGrid = new() { Left = 20, Top = 120, Width = 840, Height = 260, AllowUserToAddRows = false, AutoGenerateColumns = false };
    private readonly Label _subtotalLabel = new() { Left = 620, Top = 400, Width = 240, Text = "Subtotal: 0" };
    private readonly Label _taxLabel = new() { Left = 620, Top = 425, Width = 240, Text = "Tax: 0" };
    private readonly Label _grandTotalLabel = new() { Left = 620, Top = 450, Width = 240, Text = "Grand Total: 0" };
    private readonly Button _saveButton = new() { Left = 20, Top = 500, Width = 120, Height = 35, Text = "Save" };
    private readonly Button _recalculateButton = new() { Left = 150, Top = 500, Width = 120, Height = 35, Text = "Recalculate" };
    private readonly Button _addStorageRowButton = new() { Left = 280, Top = 500, Width = 150, Height = 35, Text = "Add Storage Line" };

    private List<Customer> _customers = new();
    private List<Product> _products = new();

    public InvoiceForm(string dbPath, int? invoiceId = null)
    {
        _invoiceId = invoiceId;

        var factory = new SqliteConnectionFactory(dbPath);
        _customerRepository = new CustomerRepository(factory);
        _productRepository = new ProductRepository(factory);
        _invoiceRepository = new InvoiceRepository(factory);

        Text = invoiceId.HasValue ? "Edit Invoice" : "New Invoice";
        Width = 900;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;

        Controls.AddRange(new Control[]
        {
            new Label { Left = 20, Top = 2, Text = "Invoice No" },
            _invoiceNumberTextBox,
            new Label { Left = 220, Top = 2, Text = "Issue Date" },
            _issueDatePicker,
            new Label { Left = 390, Top = 2, Text = "Customer" },
            _customerComboBox,
            new Label { Left = 20, Top = 52, Text = "Date In" },
            _dateInPicker,
            new Label { Left = 190, Top = 52, Text = "Date Out" },
            _dateOutPicker,
            new Label { Left = 360, Top = 52, Text = "Daily Rate" },
            _dailyRateInput,
            _storageDaysLabel,
            _itemsGrid,
            _subtotalLabel,
            _taxLabel,
            _grandTotalLabel,
            _saveButton,
            _recalculateButton,
            _addStorageRowButton
        });

        BuildGrid();

        Load += (_, _) => InitializeForm();
        _recalculateButton.Click += (_, _) => Recalculate();
        _saveButton.Click += (_, _) => SaveInvoice();
        _addStorageRowButton.Click += (_, _) => AddStorageRow();
        _dateInPicker.ValueChanged += (_, _) => Recalculate();
        _dateOutPicker.ValueChanged += (_, _) => Recalculate();
        _dailyRateInput.ValueChanged += (_, _) => Recalculate();
        _itemsGrid.CellEndEdit += (_, _) => Recalculate();
    }

    private void BuildGrid()
    {
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Description", Name = "Description", Width = 240 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Qty", Name = "Quantity", Width = 70 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit", Name = "UnitName", Width = 80 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit Price", Name = "UnitPriceMinor", Width = 110 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tax %", Name = "TaxRate", Width = 70 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", Name = "PricingRuleType", Width = 110 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Storage Days", Name = "StorageDays", Width = 90, ReadOnly = true });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Line Total", Name = "LineTotalMinor", Width = 110, ReadOnly = true });
    }

    private void InitializeForm()
    {
        _customers = _customerRepository.GetAll();
        if (_customers.Count == 0)
        {
            _customerRepository.Save(new Customer { Name = "Walk-in Customer", Country = "Algeria" });
            _customers = _customerRepository.GetAll();
        }

        _customerComboBox.DataSource = _customers;
        _customerComboBox.DisplayMember = nameof(Customer.Name);
        _customerComboBox.ValueMember = nameof(Customer.Id);

        _products = _productRepository.GetAllActive();

        if (_invoiceId.HasValue)
        {
            LoadExistingInvoice(_invoiceId.Value);
        }
        else
        {
            _invoiceNumberTextBox.Text = $"FAC-{DateTime.Now.Year}-{DateTime.Now:MMddHHmm}";
            _issueDatePicker.Value = DateTime.Today;
            _dateInPicker.Value = DateTime.Today;
            _dateOutPicker.Value = DateTime.Today;
            AddDefaultFixedRow();
            AddStorageRow();
        }

        Recalculate();
    }

    private void LoadExistingInvoice(int invoiceId)
    {
        var invoice = _invoiceRepository.GetById(invoiceId);
        if (invoice is null)
        {
            MessageBox.Show(this, $"Invoice #{invoiceId} was not found.", "Missing invoice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _invoiceNumberTextBox.Text = invoice.InvoiceNumber;
        _issueDatePicker.Value = invoice.IssueDate;
        _dateInPicker.Value = invoice.DateIn ?? invoice.IssueDate;
        _dateOutPicker.Value = invoice.DateOut ?? invoice.IssueDate;
        _customerComboBox.SelectedValue = invoice.CustomerId;
        _itemsGrid.Rows.Clear();

        foreach (var item in invoice.Items.OrderBy(i => i.LineNo))
        {
            if (item.PricingRuleType == PricingRuleType.StorageDaily)
                _dailyRateInput.Value = Math.Min(_dailyRateInput.Maximum, item.UnitPriceMinor);

            _itemsGrid.Rows.Add(
                item.Description,
                item.Quantity,
                item.UnitName,
                item.UnitPriceMinor,
                item.TaxRate,
                item.PricingRuleType,
                item.StorageDays,
                item.LineTotalMinor);
        }

        if (_itemsGrid.Rows.Count == 0)
        {
            AddDefaultFixedRow();
            AddStorageRow();
        }
    }

    private void AddDefaultFixedRow()
    {
        var fixedProduct = _products.FirstOrDefault(p => p.PricingRuleType == PricingRuleType.FixedPrice);
        if (fixedProduct is null) return;

        _itemsGrid.Rows.Add(
            fixedProduct.Name,
            1,
            fixedProduct.UnitName,
            fixedProduct.DefaultUnitPriceMinor,
            fixedProduct.TaxRate,
            PricingRuleType.FixedPrice,
            0,
            0);
    }

    private void AddStorageRow()
    {
        var storageProduct = _products.FirstOrDefault(p => p.PricingRuleType == PricingRuleType.StorageDaily);
        if (storageProduct is null) return;

        foreach (DataGridViewRow row in _itemsGrid.Rows)
        {
            var typeText = row.Cells[5].Value?.ToString() ?? string.Empty;
            if (typeText.Contains(nameof(PricingRuleType.StorageDaily), StringComparison.OrdinalIgnoreCase))
                return;
        }

        _dailyRateInput.Value = storageProduct.DefaultUnitPriceMinor;
        _itemsGrid.Rows.Add(
            storageProduct.Name,
            1,
            storageProduct.UnitName,
            storageProduct.DefaultUnitPriceMinor,
            storageProduct.TaxRate,
            PricingRuleType.StorageDaily,
            0,
            0);
    }

    private void Recalculate()
    {
        try
        {
            var invoice = BuildInvoiceFromForm();
            invoice.StorageDays = StorageDaysCalculator.ComputeBillableDays(invoice.DateIn ?? DateTime.Today, invoice.DateOut ?? DateTime.Today);

            _calculator.CalculateInvoiceTotals(invoice);
            _storageDaysLabel.Text = $"Storage Days: {invoice.StorageDays}";

            for (int i = 0; i < invoice.Items.Count && i < _itemsGrid.Rows.Count; i++)
            {
                _itemsGrid.Rows[i].Cells[6].Value = invoice.Items[i].StorageDays;
                _itemsGrid.Rows[i].Cells[7].Value = invoice.Items[i].LineTotalMinor;
            }

            _subtotalLabel.Text = $"Subtotal: {invoice.Totals.SubtotalMinor} DZD";
            _taxLabel.Text = $"Tax: {invoice.Totals.TaxTotalMinor} DZD";
            _grandTotalLabel.Text = $"Grand Total: {invoice.Totals.GrandTotalMinor} DZD";
        }
        catch
        {
            _storageDaysLabel.Text = "Storage Days: error";
        }
    }

    private Invoice BuildInvoiceFromForm()
    {
        var invoice = new Invoice
        {
            Id = _invoiceId ?? 0,
            InvoiceNumber = _invoiceNumberTextBox.Text.Trim(),
            IssueDate = _issueDatePicker.Value.Date,
            CustomerId = _customerComboBox.SelectedValue is int customerId ? customerId : _customers.FirstOrDefault()?.Id ?? 1,
            Status = "draft",
            Currency = "DZD",
            DateIn = _dateInPicker.Value.Date,
            DateOut = _dateOutPicker.Value.Date
        };

        int lineNo = 1;
        foreach (DataGridViewRow row in _itemsGrid.Rows)
        {
            if (row.IsNewRow) continue;

            var description = row.Cells[0].Value?.ToString();
            if (string.IsNullOrWhiteSpace(description)) continue;

            var quantity = decimal.TryParse(row.Cells[1].Value?.ToString(), out var q) ? q : 1m;
            var unitName = row.Cells[2].Value?.ToString() ?? "pcs";
            var unitPrice = long.TryParse(row.Cells[3].Value?.ToString(), out var up) ? up : 0;
            var taxRate = decimal.TryParse(row.Cells[4].Value?.ToString(), out var t) ? t : 0m;
            var typeText = row.Cells[5].Value?.ToString() ?? PricingRuleType.FixedPrice.ToString();
            var pricingRuleType = Enum.TryParse<PricingRuleType>(typeText, out var parsedType) ? parsedType : PricingRuleType.FixedPrice;

            var item = new InvoiceItem
            {
                LineNo = lineNo++,
                Description = description,
                Quantity = quantity,
                UnitName = unitName,
                UnitPriceMinor = pricingRuleType == PricingRuleType.StorageDaily ? (long)_dailyRateInput.Value : unitPrice,
                TaxRate = taxRate,
                PricingRuleType = pricingRuleType,
                StorageStartDate = pricingRuleType == PricingRuleType.StorageDaily ? _dateInPicker.Value.Date : null,
                StorageEndDate = pricingRuleType == PricingRuleType.StorageDaily ? _dateOutPicker.Value.Date : null
            };

            invoice.Items.Add(item);
        }

        return invoice;
    }

    private void SaveInvoice()
    {
        try
        {
            var invoice = BuildInvoiceFromForm();
            invoice.StorageDays = StorageDaysCalculator.ComputeBillableDays(invoice.DateIn ?? DateTime.Today, invoice.DateOut ?? DateTime.Today);
            _calculator.CalculateInvoiceTotals(invoice);
            _invoiceRepository.Save(invoice);
            MessageBox.Show(this, _invoiceId.HasValue ? "Invoice updated." : "Invoice saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
