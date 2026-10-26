using ComputerStore.Admin.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerStore.Admin.Forms
{
    public partial class ProductDetailDialog : Form
    {
        public ProductDetailDialog(ProductDto product)
        {
            InitializeComponent();
            SetupProductView(product);
        }

        private void SetupProductView(ProductDto p)
        {
            this.Text = $"Thông Tin Chi Tiết - {p.Name}";
            this.Size = new Size(680, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(14, 21, 22);
            this.ForeColor = Color.White;

            // 1. Ảnh sản phẩm
            PictureBox pic = new PictureBox
            {
                Location = new Point(25, 25),
                Size = new Size(240, 240),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(8, 12, 13)
            };
            if (!string.IsNullOrWhiteSpace(p.ImageUrl))
            {
                try { pic.LoadAsync(p.ImageUrl); } catch { }
            }
            this.Controls.Add(pic);

            // 2. Tên máy tính (UseMnemonic = false để không bị nuốt dấu &)
            Label lblName = new Label
            {
                Text = p.Name,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(93, 242, 214),
                Location = new Point(285, 25),
                Size = new Size(360, 55)
            };
            this.Controls.Add(lblName);

            // 3. Mã máy
            Label lblId = new Label
            {
                Text = $"Mã hệ thống: #{p.ProductId}",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 175, 175),
                Location = new Point(285, 85),
                AutoSize = true
            };
            this.Controls.Add(lblId);

            // 4. Giá bán
            Label lblPrice = new Label
            {
                Text = $"Giá niêm yết: {p.Price:N0} đ",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Location = new Point(285, 120),
                AutoSize = true
            };
            this.Controls.Add(lblPrice);

            // 5. Tồn kho
            string stockText = p.StockQuantity > 0 ? $"Còn hàng ({p.StockQuantity} máy)" : "Hết hàng";
            Label lblStock = new Label
            {
                Text = $"Tình trạng kho: {stockText}",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = p.StockQuantity > 0 ? Color.FromArgb(52, 211, 153) : Color.FromArgb(248, 113, 113),
                Location = new Point(285, 160),
                AutoSize = true
            };
            this.Controls.Add(lblStock);

            // 6. Nhóm danh mục
            Label lblCategory = new Label
            {
                Text = $"Mã danh mục: {p.CategoryId ?? 1}",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(160, 175, 175),
                Location = new Point(285, 195),
                AutoSize = true
            };
            this.Controls.Add(lblCategory);

            // 7. Khung thông số kỹ thuật (Specs)
            Label lblSpecsTitle = new Label
            {
                Text = "THÔNG SỐ KỸ THUẬT && CẤU HÌNH CHI TIẾT:",
                UseMnemonic = false,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(93, 242, 214),
                Location = new Point(25, 285),
                AutoSize = true
            };
            this.Controls.Add(lblSpecsTitle);

            TextBox txtDetails = new TextBox
            {
                Text = string.IsNullOrWhiteSpace(p.Description) ? "Chưa có thông tin cấu hình chi tiết." : p.Description,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(20, 31, 33),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(25, 315),
                Size = new Size(615, 130)
            };
            this.Controls.Add(txtDetails);

            // 8. Nút đóng
            Button btnClose = new Button
            {
                Text = "Đóng",
                DialogResult = DialogResult.OK,
                Size = new Size(110, 38),
                Location = new Point(530, 465),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 121, 107),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(93, 242, 214);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void ProductDetailDialog_Load(object sender, EventArgs e)
        {
        }

        private void ProductDetailDialog_Load_1(object sender, EventArgs e)
        {

        }
    }
}