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
    private readonly InvoicePrintDocumentBuilder _printBuilder = new();
    private readonly InvoicePdfExporter _pdfExporter = new();
    private readonly int? _invoiceId;

    private readonly TextBox _invoiceNumberTextBox = new() { Left = 20, Top = 20, Width = 180 };
    private readonly DateTimePicker _issueDatePicker = new() { Left = 220, Top = 20, Width = 150 };
    private readonly ComboBox _customerComboBox = new() { Left = 390, Top = 20, Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _addCustomerButton = new() { Left = 590, Top = 18, Width = 90, Height = 28, Text = "New Client" };
    private readonly DateTimePicker _dateInPicker = new() { Left = 20, Top = 70, Width = 150 };
    private readonly DateTimePicker _dateOutPicker = new() { Left = 190, Top = 70, Width = 150 };
    private readonly NumericUpDown _dailyRateInput = new() { Left = 360, Top = 70, Width = 120, DecimalPlaces = 0, Maximum = 100000000 };
    private readonly Label _storageDaysLabel = new() { Left = 500, Top = 74, Width = 180, Text = "Storage Days: 0" };
    private readonly DataGridView _itemsGrid = new() { Left = 20, Top = 120, Width = 840, Height = 260, AllowUserToAddRows = false, AutoGenerateColumns = false, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
    private readonly Label _subtotalLabel = new() { Left = 620, Top = 400, Width = 240, Text = "Subtotal: 0" };
    private readonly Label _taxLabel = new() { Left = 620, Top = 425, Width = 240, Text = "Tax: 0" };
    private readonly Label _grandTotalLabel = new() { Left = 620, Top = 450, Width = 240, Text = "Grand Total: 0" };
    private readonly Button _saveButton = new() { Left = 20, Top = 500, Width = 120, Height = 35, Text = "Save" };
    private readonly Button _recalculateButton = new() { Left = 150, Top = 500, Width = 120, Height = 35, Text = "Recalculate" };
    private readonly Button _addItemButton = new() { Left = 280, Top = 500, Width = 120, Height = 35, Text = "Add Item" };
    private readonly Button _addStorageRowButton = new() { Left = 410, Top = 500, Width = 150, Height = 35, Text = "Add Storage Line" };
    private readonly Button _previewButton = new() { Left = 570, Top = 500, Width = 140, Height = 35, Text = "Preview TXT" };
    private readonly Button _exportPdfButton = new() { Left = 720, Top = 500, Width = 140, Height = 35, Text = "Export PDF" };

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
        MinimumSize = new Size(920, 660);
        StartPosition = FormStartPosition.CenterParent;

        Controls.AddRange(new Control[]
        {
            new Label { Left = 20, Top = 2, Text = "Invoice No" },
            _invoiceNumberTextBox,
            new Label { Left = 220, Top = 2, Text = "Issue Date" },
            _issueDatePicker,
            new Label { Left = 390, Top = 2, Text = "Customer" },
            _customerComboBox,
            _addCustomerButton,
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
            _addItemButton,
            _addStorageRowButton,
            _previewButton,
            _exportPdfButton
        });

        BuildGrid();

        Load += (_, _) => InitializeForm();
        _recalculateButton.Click += (_, _) => Recalculate();
        _saveButton.Click += (_, _) => SaveInvoice();
        _addItemButton.Click += (_, _) => AddManualItemRow();
        _addStorageRowButton.Click += (_, _) => AddStorageRow();
        _addCustomerButton.Click += (_, _) => AddCustomer();
        _previewButton.Click += (_, _) => PreviewInvoice();
        _exportPdfButton.Click += (_, _) => ExportPdf();
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
        if (storageProduct is null)
        {
            MessageBox.Show(this, "No storage product is configured yet.", "Missing product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        foreach (DataGridViewRow row in _itemsGrid.Rows)
        {
            var typeText = row.Cells[5].Value?.ToString() ?? string.Empty;
            if (typeText.Contains(nameof(PricingRuleType.StorageDaily), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "This invoice already has a storage line. Use Add Item for extra manual lines.", "Storage line exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
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

    private void AddManualItemRow()
    {
        var fixedProduct = _products.FirstOrDefault(p => p.PricingRuleType == PricingRuleType.FixedPrice);

        _itemsGrid.Rows.Add(
            fixedProduct?.Name ?? "New item",
            1,
            fixedProduct?.UnitName ?? "pcs",
            fixedProduct?.DefaultUnitPriceMinor ?? 0,
            fixedProduct?.TaxRate ?? 0m,
            PricingRuleType.FixedPrice,
            0,
            0);

        var newRowIndex = _itemsGrid.Rows.Count - 1;
        if (newRowIndex >= 0)
        {
            _itemsGrid.CurrentCell = _itemsGrid.Rows[newRowIndex].Cells[0];
            _itemsGrid.BeginEdit(true);
        }
    }

    private void AddCustomer()
    {
        using var dialog = new AddCustomerForm();
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var customer = new Customer
        {
            Name = dialog.CustomerName.Trim(),
            Phone = string.IsNullOrWhiteSpace(dialog.Phone) ? null : dialog.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(dialog.Email) ? null : dialog.Email.Trim(),
            Country = string.IsNullOrWhiteSpace(dialog.Country) ? "Algeria" : dialog.Country.Trim(),
            IsActive = true
        };

        var customerId = _customerRepository.Save(customer);
        _customers = _customerRepository.GetAll();
        _customerComboBox.DataSource = null;
        _customerComboBox.DataSource = _customers;
        _customerComboBox.DisplayMember = nameof(Customer.Name);
        _customerComboBox.ValueMember = nameof(Customer.Id);
        _customerComboBox.SelectedValue = customerId;
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

    private void PreviewInvoice()
    {
        try
        {
            var invoice = BuildPreparedInvoice();
            var customerName = GetCustomerName(invoice.CustomerId);
            var previewText = _printBuilder.BuildPlainText(invoice, customerName);

            using var dialog = new SaveFileDialog
            {
                Title = "Export invoice preview",
                Filter = "Text file (*.txt)|*.txt",
                FileName = $"{invoice.InvoiceNumber}-preview.txt"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                File.WriteAllText(dialog.FileName, previewText);
                MessageBox.Show(this, "Text preview exported.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Preview failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportPdf()
    {
        try
        {
            var invoice = BuildPreparedInvoice();
            var customerName = GetCustomerName(invoice.CustomerId);

            using var dialog = new SaveFileDialog
            {
                Title = "Export invoice PDF",
                Filter = "PDF file (*.pdf)|*.pdf",
                FileName = $"{invoice.InvoiceNumber}.pdf"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _pdfExporter.Export(dialog.FileName, invoice, customerName);
                MessageBox.Show(this, "PDF exported.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "PDF export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Invoice BuildPreparedInvoice()
    {
        var invoice = BuildInvoiceFromForm();
        invoice.StorageDays = StorageDaysCalculator.ComputeBillableDays(invoice.DateIn ?? DateTime.Today, invoice.DateOut ?? DateTime.Today);
        _calculator.CalculateInvoiceTotals(invoice);
        return invoice;
    }

    private string GetCustomerName(int customerId)
    {
        return _customers.FirstOrDefault(c => c.Id == customerId)?.Name ?? "Unknown customer";
    }

    private void SaveInvoice()
    {
        try
        {
            var invoice = BuildPreparedInvoice();
            _invoiceRepository.Save(invoice);
            MessageBox.Show(this, _invoiceId.HasValue ? "Invoice updated." : "Invoice saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private sealed class AddCustomerForm : Form
    {
        private readonly TextBox _nameTextBox = new() { Left = 120, Top = 20, Width = 220 };
        private readonly TextBox _phoneTextBox = new() { Left = 120, Top = 55, Width = 220 };
        private readonly TextBox _emailTextBox = new() { Left = 120, Top = 90, Width = 220 };
        private readonly TextBox _countryTextBox = new() { Left = 120, Top = 125, Width = 220, Text = "Algeria" };

        public string CustomerName => _nameTextBox.Text;
        public string Phone => _phoneTextBox.Text;
        public string Email => _emailTextBox.Text;
        public string Country => _countryTextBox.Text;

        public AddCustomerForm()
        {
            Text = "New Client";
            Width = 390;
            Height = 240;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var saveButton = new Button { Text = "Save", Left = 120, Top = 165, Width = 100, DialogResult = DialogResult.OK };
            var cancelButton = new Button { Text = "Cancel", Left = 240, Top = 165, Width = 100, DialogResult = DialogResult.Cancel };

            saveButton.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
                {
                    MessageBox.Show(this, "Please enter the client name.", "Missing name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };

            Controls.AddRange(new Control[]
            {
                new Label { Left = 20, Top = 24, Width = 90, Text = "Client Name" },
                _nameTextBox,
                new Label { Left = 20, Top = 59, Width = 90, Text = "Phone" },
                _phoneTextBox,
                new Label { Left = 20, Top = 94, Width = 90, Text = "Email" },
                _emailTextBox,
                new Label { Left = 20, Top = 129, Width = 90, Text = "Country" },
                _countryTextBox,
                saveButton,
                cancelButton
            });

            AcceptButton = saveButton;
            CancelButton = cancelButton;
        }
    }
}
