using ComputerStore.Admin.Controls;
using ComputerStore.Admin.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerStore.Admin.Forms;

// Giữ tên lớp để tương thích với Designer; trang này được nhúng vào MainForm.
public partial class ProductDetailDialog : Form
{
    public event Action? BackRequested;

    public ProductDetailDialog(ProductDto product)
    {
        InitializeComponent();
        SetupProductView(product);
    }

    private void SetupProductView(ProductDto product)
    {
        Text = "BMTDStore - Chi tiết sản phẩm";
        Controls.Clear();
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 2, RowCount = 5,
            BackColor = AdminTheme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        var title = new Label
        {
            Text = "CHI TIẾT SẢN PHẨM", Dock = DockStyle.Fill,
            ForeColor = AdminTheme.Accent, Font = new Font("Segoe UI", 17, FontStyle.Bold)
        };
        layout.Controls.Add(title, 0, 0); layout.SetColumnSpan(title, 2);
        var picture = new PictureBox
        {
            Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = AdminTheme.Surface, Margin = new Padding(0, 0, 20, 12)
        };
        if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            try { picture.LoadAsync(ApiService.ResolveImageUrl(product.ImageUrl)!); }
            catch { /* Ảnh lỗi không cản trở xem thông tin sản phẩm. */ }
        }
        layout.Controls.Add(picture, 0, 1);
        var info = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = AdminTheme.Surface, Padding = new Padding(16) };
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for (int i = 0; i < 4; i++) info.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        AddInfo(product.Name, 15, AdminTheme.Accent);
        AddInfo($"Mã sản phẩm: {product.ProductId}", 10, Color.White);
        AddInfo($"Giá bán: {product.Price:N0} đ", 13, AdminTheme.Accent);
        AddInfo(product.StockQuantity > 0 ? $"Còn hàng: {product.StockQuantity} máy" : "Hết hàng", 11,
            product.StockQuantity > 0 ? AdminTheme.Accent : AdminTheme.Danger);
        AddInfo($"Mã danh mục: {product.CategoryId?.ToString() ?? "Chưa phân loại"}", 10, Color.Silver);
        layout.Controls.Add(info, 1, 1);
        var specLabel = new Label { Text = "THÔNG SỐ KỸ THUẬT VÀ CẤU HÌNH", Dock = DockStyle.Fill, ForeColor = AdminTheme.Accent, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        layout.Controls.Add(specLabel, 0, 2); layout.SetColumnSpan(specLabel, 2);
        var specs = new TextBox
        {
            Text = product.Specifications ?? product.Description ?? "Chưa có thông tin cấu hình.",
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11)
        };
        layout.Controls.Add(specs, 0, 3); layout.SetColumnSpan(specs, 2);
        var back = new ComputerStore.Admin.Controls.AdminButton { Text = "Quay lại sản phẩm", Width = 190, Height = 38, Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
        back.Click += (_, _) => BackRequested?.Invoke();
        layout.Controls.Add(back, 0, 4);
        Controls.Add(layout);
        AdminTheme.Apply(this);

        void AddInfo(string text, float size, Color color)
        {
            info.Controls.Add(new Label { Text = text, UseMnemonic = false, Dock = DockStyle.Fill, ForeColor = color,
                Font = new Font("Segoe UI", size, size >= 13 ? FontStyle.Bold : FontStyle.Regular), AutoEllipsis = true });
        }
    }

    private void ProductDetailDialog_Load(object sender, EventArgs e) { }
    private void ProductDetailDialog_Load_1(object sender, EventArgs e) { }
}
