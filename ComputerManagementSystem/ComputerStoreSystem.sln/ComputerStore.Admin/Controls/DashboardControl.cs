using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComputerStore.Admin.Services;

namespace ComputerStore.Admin.Controls;

public sealed class DashboardControl : UserControl
{
    private static readonly Color Accent = Color.FromArgb(80, 239, 211);
    private readonly ApiService _api = new();
    private readonly CancellationTokenSource _requests = new();
    private readonly ComboBox _period = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
    private readonly DateTimePicker _from = DatePicker();
    private readonly DateTimePicker _to = DatePicker();
    private readonly Button _refresh = new ComputerStore.Admin.Controls.AdminButton() { Text = "Làm mới", AutoSize = true, Height = 32 };
    private readonly Label _message = new() { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label[] _metrics = new Label[4];
    private readonly TableLayoutPanel _body = new();
    private readonly DashboardChart _revenue = new() { Dock = DockStyle.Fill };
    private readonly DashboardChart _statuses = new() { Dock = DockStyle.Fill, HorizontalBars = true };
    private readonly DataGridView _best = CreateGrid();
    private readonly DataGridView _stock = CreateGrid();
    private readonly DataGridView _recent = CreateGrid();
    private readonly Label _stockTitle = new();
    private DashboardData? _data;
    private bool _busy;
    private bool _disposed;
    private bool _selectingPeriod;

    public event Action<int>? OpenOrderRequested;
    public event Action<int>? OpenProductRequested;

    public DashboardControl()
    {
        BackColor = Color.FromArgb(8, 12, 13);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        Dock = DockStyle.Fill;
        AutoScroll = true;

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1,
            Padding = new Padding(20), MinimumSize = new Size(800, 0)
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(page, Heading("TỔNG QUAN KINH DOANH", 18), 50);

        var filters = AdminLayout.Toolbar(
            AdminLayout.Field(new Label { Text = "Khoảng thời gian" }, _period, 180),
            AdminLayout.Field(new Label { Text = "Từ ngày" }, _from, 150),
            AdminLayout.Field(new Label { Text = "Đến ngày" }, _to, 150));
        _period.Items.AddRange(new object[] { "Hôm nay", "7 ngày qua", "Tháng này", "Tùy chọn" });
        _refresh.Text = "Làm mới dữ liệu";
        AdminTheme.StyleButton(_refresh);
        _refresh.Margin = new Padding(0, 22, 0, 10);
        filters.Controls.Add(_refresh);
        AddRow(page, filters, null);
        _message.Text = "Chưa tải dữ liệu.";
        _message.ForeColor = Color.Silver;
        _message.AutoSize = true;
        _message.Padding = new Padding(0, 8, 0, 12);
        AddRow(page, _message, null);

        _body.Dock = DockStyle.Fill;
        _body.AutoSize = true;
        _body.ColumnCount = 1;
        _body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _body.Visible = false;
        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        string[] titles = { "DOANH THU HOÀN THÀNH", "ĐƠN MỚI TRONG KỲ", "ĐƠN CHỜ TRONG KỲ", "KHÁCH HÀNG HIỆN CÓ" };
        for (int index = 0; index < 4; index++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(14, 21, 22), Margin = new Padding(0, 0, 10, 0) };
            card.Controls.Add(new Label
            {
                Text = titles[index], Dock = DockStyle.Top, Height = 34,
                Padding = new Padding(12, 8, 0, 0), Font = new Font("Segoe UI", 9)
            });
            _metrics[index] = new Label
            {
                Text = "—", Dock = DockStyle.Bottom, Height = 58,
                Padding = new Padding(12, 4, 0, 0), ForeColor = Accent,
                Font = new Font("Segoe UI", index == 0 ? 14 : 19, FontStyle.Bold), AutoEllipsis = true
            };
            card.Controls.Add(_metrics[index]);
            cards.Controls.Add(card, index, 0);
        }
        AddRow(_body, cards, 104);

        var charts = TwoColumns(62);
        charts.Controls.Add(Section("DOANH THU THEO NGÀY ĐẶT • ĐƠN ĐÃ HOÀN THÀNH", _revenue), 0, 0);
        charts.Controls.Add(Section("TRẠNG THÁI ĐƠN TRONG KỲ", _statuses), 1, 0);
        AddRow(_body, charts, 290);

        ConfigureColumns(_best, ("ProductId", "Mã SP", 55), ("ProductName", "Sản phẩm", 220),
            ("Quantity", "Đã bán", 65), ("Revenue", "Doanh thu (đ)", 115));
        _best.Columns["Revenue"]!.DefaultCellStyle.Format = "N0";
        ConfigureColumns(_stock, ("ProductId", "Mã SP", 55), ("Name", "Sản phẩm", 200),
            ("StockQuantity", "Tồn", 55), ("StockStatus", "Tình trạng", 100));
        var tables = TwoColumns(55);
        tables.Controls.Add(Section("TOP 5 BÁN CHẠY • ĐƠN HOÀN THÀNH TRONG KỲ", _best), 0, 0);
        tables.Controls.Add(Section("TỒN KHO HIỆN TẠI • SẮP HẾT TỪ 1–3 MÁY", _stock, _stockTitle), 1, 0);
        AddRow(_body, tables, 290);

        ConfigureColumns(_recent, ("OrderId", "Mã đơn", 65), ("OrderDate", "Ngày đặt", 135),
            ("CustomerName", "Khách hàng", 170), ("TotalAmount", "Tổng tiền (đ)", 130), ("Status", "Trạng thái", 145));
        _recent.Columns["OrderDate"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        _recent.Columns["TotalAmount"]!.DefaultCellStyle.Format = "N0";
        AddRow(_body, Section("10 ĐƠN GẦN NHẤT TRONG KỲ • NHẤP ĐÚP ĐỂ MỞ", _recent), 330);
        AddRow(page, _body, null);
        Controls.Add(page);

        _period.SelectedIndexChanged += (_, _) => SelectPeriod();
        _from.ValueChanged += (_, _) => DatesChanged();
        _to.ValueChanged += (_, _) => DatesChanged();
        _refresh.Click += async (_, _) => await ReloadAsync();
        _recent.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _recent.Rows[e.RowIndex].DataBoundItem is OrderDto order)
                OpenOrderRequested?.Invoke(order.OrderId);
        };
        _stock.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _stock.Rows[e.RowIndex].DataBoundItem is ProductDto product)
                OpenProductRequested?.Invoke(product.ProductId);
        };
        _best.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _best.Rows[e.RowIndex].DataBoundItem is DashboardBestSeller product)
                OpenProductRequested?.Invoke(product.ProductId);
        };
        _period.SelectedIndex = 2;
    }

    private static DateTime TodayInVietnam => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;
    private static DateTimePicker DatePicker() => new()
    {
        Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy",
        Width = 116, Margin = new Padding(4, 0, 8, 8)
    };

    private void SelectPeriod()
    {
        if (_selectingPeriod) return;
        _selectingPeriod = true;
        try
        {
            var today = TodayInVietnam;
            switch (_period.SelectedIndex)
            {
                case 0: _from.Value = today; _to.Value = today; break;
                case 1: _from.Value = today.AddDays(-6); _to.Value = today; break;
                case 2: _from.Value = new DateTime(today.Year, today.Month, 1); _to.Value = today; break;
            }
        }
        finally { _selectingPeriod = false; }
        RenderData();
    }

    private void DatesChanged()
    {
        if (_selectingPeriod) return;
        _selectingPeriod = true;
        _period.SelectedIndex = 3;
        _selectingPeriod = false;
        RenderData();
    }

    public async Task ReloadAsync()
    {
        if (_busy || IsDisposed) return;
        _busy = true;
        SetFiltersEnabled(false);
        _message.ForeColor = Color.Silver;
        _message.Text = "Đang tải đơn hàng, sản phẩm và tài khoản…";
        _refresh.Text = "Đang tải…";
        _body.Enabled = false;
        try
        {
            var loaded = await _api.GetDashboardDataAsync(_requests.Token);
            if (IsDisposed || _requests.IsCancellationRequested) return;
            _data = loaded;
            RenderData();
        }
        catch (OperationCanceledException) when (_requests.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (IsDisposed || _requests.IsCancellationRequested) return;
            _data = null;
            _body.Visible = false;
            _message.ForeColor = Color.FromArgb(255, 185, 90);
            _message.Text = "Tải thất bại. " + ex.Message + " Bấm Làm mới để thử lại.";
        }
        finally
        {
            _busy = false;
            if (!IsDisposed)
            {
                _refresh.Text = "Làm mới dữ liệu";
                _body.Enabled = true;
                SetFiltersEnabled(true);
            }
        }
    }

    private void SetFiltersEnabled(bool enabled)
    {
        _period.Enabled = enabled;
        _from.Enabled = enabled;
        _to.Enabled = enabled;
        _refresh.Enabled = enabled;
    }

    private void RenderData()
    {
        if (_data == null || IsDisposed) return;
        if (_from.Value.Date > _to.Value.Date || (_to.Value.Date - _from.Value.Date).TotalDays > 365)
        {
            _body.Visible = false;
            _message.ForeColor = Color.FromArgb(255, 185, 90);
            _message.Text = "Chọn ngày bắt đầu trước hoặc bằng ngày kết thúc, tối đa 366 ngày.";
            return;
        }
        var report = DashboardCalculator.Calculate(_data, _from.Value, _to.Value);
        var money = report.Revenue.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
        _metrics[0].Text = money;
        _metrics[1].Text = report.OrderCount.ToString("N0");
        _metrics[2].Text = report.PendingCount.ToString("N0");
        _metrics[3].Text = _data.CustomerCount.ToString("N0");
        _revenue.SetData(report.DailyRevenue);
        _statuses.SetData(report.OrderStatuses);
        _best.DataSource = report.BestSellers;
        var stock = _data.Products.Where(product => product.StockQuantity <= 3)
            .OrderBy(product => product.StockQuantity).ThenBy(product => product.ProductId).ToList();
        _stock.DataSource = stock;
        _stockTitle.Text = "TỒN KHO HIỆN TẠI • " + stock.Count + " MẪU CẦN CHÚ Ý";
        _recent.DataSource = report.RecentOrders;
        _message.ForeColor = Color.Silver;
        _message.Text = "Cập nhật " + _data.LoadedAt.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss") +
            " • " + report.CompletedCount + " đơn hoàn thành trong kỳ.\n" +
            "Doanh thu phân theo ngày đặt; tồn kho và khách hàng là số liệu hiện tại. Nhấp đúp sản phẩm để mở quản lý.";
        _body.Visible = true;
    }

    private static Label Heading(string text, float size) => new()
    {
        Text = text, Dock = DockStyle.Fill, ForeColor = Accent,
        Font = new Font("Segoe UI", size, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
    };
    private static Label SmallLabel(string text) => new()
    {
        Text = text, AutoSize = true, Margin = new Padding(0, 6, 5, 8)
    };
    private static TableLayoutPanel TwoColumns(int leftPercent)
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, leftPercent));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 - leftPercent));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        return table;
    }
    private static void AddRow(TableLayoutPanel table, Control control, int? height)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(height.HasValue ? new RowStyle(SizeType.Absolute, height.Value) :
            new RowStyle(SizeType.AutoSize));
        table.Controls.Add(control, 0, row);
    }
    private static Panel Section(string title, Control content, Label? header = null)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill, BackColor = Color.FromArgb(14, 21, 22),
            Padding = new Padding(8), Margin = new Padding(0, 10, 10, 0)
        };
        header ??= new Label();
        header.Text = title;
        header.Dock = DockStyle.Top;
        header.Height = 52;
        header.AutoEllipsis = true;
        header.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        header.ForeColor = Accent;
        header.TextAlign = ContentAlignment.MiddleLeft;
        content.Dock = DockStyle.Fill;
        panel.Controls.Add(content);
        panel.Controls.Add(header);
        return panel;
    }
    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill, AutoGenerateColumns = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false, ReadOnly = true, RowHeadersVisible = false,
        MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Color.FromArgb(14, 21, 22), BorderStyle = BorderStyle.None,
        GridColor = Color.FromArgb(30, 69, 62), EnableHeadersVisualStyles = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersHeight = 38,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        RowTemplate = { Height = 34 },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(14, 21, 22), ForeColor = Color.White,
            SelectionBackColor = Color.FromArgb(0, 100, 87), SelectionForeColor = Color.White,
            NullValue = "—", FormatProvider = CultureInfo.GetCultureInfo("vi-VN")
        },
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(20, 61, 53), ForeColor = Accent,
            WrapMode = DataGridViewTriState.True,
            Padding = new Padding(4, 7, 4, 7)
        }
    };
    private static void ConfigureColumns(DataGridView grid, params (string Property, string Title, int Width)[] columns)
    {
        foreach (var column in columns)
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = column.Property, DataPropertyName = column.Property, HeaderText = column.Title,
                FillWeight = column.Width, MinimumWidth = Math.Min(100, column.Width),
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _requests.Cancel();
            _requests.Dispose();
        }
        base.Dispose(disposing);
    }
}
