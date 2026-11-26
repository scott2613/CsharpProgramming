using ComputerStore.Admin.Controls;
using ComputerStore.Admin.Forms;
using ComputerStore.Admin.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerStore.Admin;

public partial class MainForm : Form
{
    private readonly DashboardControl _dashboard = new();
    private readonly Panel _content = new() { Dock = DockStyle.Fill, AutoScroll = false };
    private readonly Dictionary<string, Button> _menuButtons = new();
    private readonly Dictionary<string, Form> _pages = new();
    private Control? _activePage;
    private ProductDetailDialog? _detailPage;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool LogoutRequested { get; private set; }

    public MainForm()
    {
        InitializeComponent();
        SetupShell();
        Shown += async (_, _) => await ShowDashboardAsync();
    }

    private void Form1_Load(object sender, EventArgs e) { }

    private void SetupShell()
    {
        Text = "BMTDStore - Quản trị";
        Size = new Size(1400, 850);
        MinimumSize = new Size(1120, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AdminTheme.Background;
        ForeColor = Color.White;
        Controls.Clear();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Padding = new Padding(12, 24, 12, 12),
            BackColor = AdminTheme.Surface, Margin = Padding.Empty
        };
        menu.Controls.Add(new Label
        {
            Text = "BMTDStore", AutoSize = false, Size = new Size(220, 60),
            Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = AdminTheme.Accent
        });
        AddMenu("dashboard", "DASHBOARD", async () => await ShowDashboardAsync());
        AddMenu("orders", "QUẢN LÝ ĐƠN HÀNG", () => { ShowOrders(); return Task.CompletedTask; });
        AddMenu("products", "QUẢN LÝ SẢN PHẨM", () => { ShowProducts(); return Task.CompletedTask; });
        AddMenu("users", "QUẢN LÝ TÀI KHOẢN", () => { ShowUsers(); return Task.CompletedTask; });
        var logout = MenuButton("ĐĂNG XUẤT");
        logout.ForeColor = AdminTheme.Danger;
        logout.Click += async (_, _) =>
        {
            if (MessageBox.Show(this, "Bạn muốn đăng xuất?", "BMTDStore",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            logout.Enabled = false;
            try { await new ApiService().LogoutAsync(); }
            catch (Exception) { MessageBox.Show(this, "API chưa xác nhận đăng xuất. Cookie trên máy đã được xóa."); }
            LogoutRequested = true;
            Close();
        };
        menu.Controls.Add(logout);
        _dashboard.OpenOrderRequested += async id => await ShowOrdersAsync(id, null);
        _dashboard.OpenProductRequested += async id =>
        {
            var page = ShowProducts();
            await page.FocusProductAsync(id);
        };
        _content.Controls.Add(_dashboard);
        _dashboard.Hide();
        AdminTheme.Apply(_dashboard);
        layout.Controls.Add(menu, 0, 0);
        layout.Controls.Add(_content, 1, 0);
        Controls.Add(layout);

        void AddMenu(string key, string title, Func<Task> action)
        {
            var button = MenuButton(title);
            _menuButtons.Add(key, button);
            button.Click += async (_, _) =>
            {
                // Vẫn cho phép chuyển mục khi Dashboard đang chờ API.
                try { await action(); }
                catch (Exception ex)
                {
                    if (!IsDisposed) MessageBox.Show(this, "Không mở được trang. " + ex.Message,
                        "BMTDStore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            menu.Controls.Add(button);
        }
    }

    private async Task ShowDashboardAsync()
    {
        ActivatePage("dashboard", _dashboard);
        _dashboard.AutoScrollPosition = Point.Empty;
        // Tải lại khi quay về để số liệu phản ánh thao tác vừa lưu ở màn hình quản lý.
        await _dashboard.ReloadAsync();
    }

    private OrderManagementForm ShowOrders()
    {
        var page = GetPage("orders", () => new OrderManagementForm());
        ActivatePage("orders", page);
        return page;
    }

    private async Task ShowOrdersAsync(int? orderId, string? customerFilter)
    {
        // Khởi tạo bằng bộ lọc cần đến; các lần sau dùng lại trang để giữ trạng thái nhập.
        if (!_pages.ContainsKey("orders"))
        {
            var created = GetPage("orders", () => orderId.HasValue
                ? new OrderManagementForm(orderId.Value) : new OrderManagementForm(customerFilter));
            ActivatePage("orders", created);
            return;
        }
        var page = ShowOrders();
        await page.NavigateAsync(orderId, customerFilter);
    }

    private ProductManagementForm ShowProducts()
    {
        var page = GetPage("products", () =>
        {
            var form = new ProductManagementForm();
            form.OpenOrdersRequested += async () => await ShowOrdersAsync(null, null);
            form.OpenProductRequested += ShowProductDetail;
            return form;
        });
        ActivatePage("products", page);
        return page;
    }

    private void ShowProductDetail(ProductDto product)
    {
        var page = new ProductDetailDialog(product);
        page.BackRequested += () => ShowProducts();
        ConfigureEmbedded(page);
        _content.Controls.Add(page);
        // ActivatePage dọn trang chi tiết cũ trước khi gán trang mới.
        ActivatePage("products", page);
        _detailPage = page;
    }

    private void ShowUsers()
    {
        var page = GetPage("users", () =>
        {
            var form = new UserManagementForm();
            form.OpenOrdersRequested += async filter => await ShowOrdersAsync(null, filter);
            return form;
        });
        ActivatePage("users", page);
    }

    private T GetPage<T>(string key, Func<T> factory) where T : Form
    {
        if (_pages.TryGetValue(key, out var existing) && !existing.IsDisposed) return (T)existing;
        var page = factory();
        ConfigureEmbedded(page);
        _pages[key] = page;
        _content.Controls.Add(page);
        page.Hide();
        return page;
    }

    private static void ConfigureEmbedded(Form page)
    {
        // Form trở thành control con; không tạo cửa sổ hay nút trên thanh tác vụ riêng.
        page.TopLevel = false;
        page.FormBorderStyle = FormBorderStyle.None;
        page.ShowInTaskbar = false;
        page.StartPosition = FormStartPosition.Manual;
        page.MinimumSize = Size.Empty;
        page.AutoScroll = false;
        page.AutoScaleMode = AutoScaleMode.None;
        page.Dock = DockStyle.Fill;
        AdminTheme.Apply(page);
    }

    private void ActivatePage(string key, Control page)
    {
        _content.SuspendLayout();
        _activePage?.Hide();
        if (_detailPage != null && _detailPage != page)
        {
            _detailPage.Dispose();
            _detailPage = null;
        }
        _content.AutoScrollPosition = Point.Empty;
        _content.AutoScrollMinSize = Size.Empty;
        page.Show();
        page.BringToFront();
        if (page is UserManagementForm accountPage) accountPage.ResetEditorScroll();
        if (page is ProductManagementForm productPage) productPage.ResetEditorScroll();
        _activePage = page;
        foreach (var entry in _menuButtons)
        {
            bool selected = entry.Key == key;
            entry.Value.BackColor = selected ? AdminTheme.Selected : AdminTheme.Input;
            entry.Value.FlatAppearance.BorderColor = selected ? AdminTheme.Accent : AdminTheme.Border;
            entry.Value.AccessibleDescription = selected ? "Trang đang mở" : "Mở trang trong khung bên phải";
        }
        _content.ResumeLayout(true);
    }

    private static Button MenuButton(string text)
    {
        var button = new ComputerStore.Admin.Controls.AdminButton
        {
            Name = "AdminMenuButton", Text = text, Width = 220, Height = 40, Margin = new Padding(0, 0, 0, 8),
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
        };
        AdminTheme.StyleButton(button);
        button.AutoSize = false;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(16, 0, 8, 0);
        return button;
    }
}
