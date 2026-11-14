using ComputerStore.Admin.Services;
using ComputerStore.Core.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerStore.Admin.Forms
{
    public partial class ProductManagementForm : Form
    {
        private readonly ApiService _apiService;
        private List<Product> _productList = new();
        private int _selectedProductId = 0;
        private bool _operationInProgress;

        public ProductManagementForm()
        {
            InitializeComponent();
            SetupCyberGridStyle();
            btnClear.Text = "Xóa ô nhập";
            btnReload.Text = "Làm mới";
            txtImageUrl.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                LoadImagePreview(txtImageUrl.Text);
            };

            _apiService = new ApiService();


        }

        private void SetupCyberGridStyle()
        {
            if (dgvProducts == null) return;

            dgvProducts.EnableHeadersVisualStyles = false;
            dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 61, 53);
            dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(93, 242, 214);
            dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvProducts.DefaultCellStyle.BackColor = Color.FromArgb(14, 21, 22);
            dgvProducts.DefaultCellStyle.ForeColor = Color.White;
            dgvProducts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 121, 107);
            dgvProducts.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvProducts.BorderStyle = BorderStyle.None;
            dgvProducts.GridColor = Color.FromArgb(30, 69, 62);
        }

        private async void ProductManagementForm_Load(object sender, EventArgs e)
        {
            await RunProductOperationAsync(LoadProductsAsync);
        }

        // Nhấp đúp vào dòng để xem chi tiết
        private void dgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OpenProductDetail();
        }

        // Nút bấm "Xem chi tiết"
        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            OpenProductDetail();
        }

        private void OpenProductDetail()
        {
            // Dùng dòng đang chọn, kể cả khi di chuyển bằng bàn phím.
            if (dgvProducts.CurrentRow != null &&
                int.TryParse(dgvProducts.CurrentRow.Cells["Mã_SP"]?.Value?.ToString(), out int currentId))
                _selectedProductId = currentId;

            if (_selectedProductId <= 0)
            {
                MessageBox.Show("Vui lòng click chọn một máy tính trong danh sách trước khi xem chi tiết!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var product = _productList.FirstOrDefault(p => p.ProductId == _selectedProductId);
            if (product != null)
            {
                var dto = new ProductDto
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    Description = product.Specifications,
                    Specifications = product.Specifications,
                    ImageUrl = product.ImageUrl,
                    CategoryId = product.CategoryId
                };
                using var detailDialog = new ProductDetailDialog(dto);
                detailDialog.ShowDialog(this);
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var loaded = await _apiService.GetProductsAsync();
                if (_apiService.LastError != null || IsDisposed) return;
                _productList = loaded;
                ClearInputs();
                RenderGrid();
                dgvProducts.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối API: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderGrid()
        {
            if (dgvProducts == null) return;

            string keyword = txtSearch?.Text.Trim().ToLower() ?? "";
            var filtered = string.IsNullOrEmpty(keyword)
                ? _productList
                : _productList.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(keyword)) ||
                    (p.Specifications != null && p.Specifications.ToLower().Contains(keyword)) ||
                    p.ProductId.ToString().Contains(keyword)
                ).ToList();

            dgvProducts.DataSource = null;
            dgvProducts.DataSource = filtered.Select(p => new
            {
                Mã_SP = p.ProductId,
                Tên_Máy = p.Name,
                Đơn_Giá = p.Price.ToString("N0") + " đ",
                Tồn_Kho = p.StockQuantity,
                Cấu_Hình = p.Specifications,
                Tình_Trạng = p.StockQuantity > 0 ? "Còn hàng" : "Hết hàng"
            }).ToList();
        }

        private void dgvProducts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvProducts == null) return;

            var row = dgvProducts.Rows[e.RowIndex];
            if (int.TryParse(row.Cells["Mã_SP"]?.Value?.ToString(), out int id))
            {
                var item = _productList.FirstOrDefault(p => p.ProductId == id);
                if (item != null)
                {
                    _selectedProductId = item.ProductId;
                    if (txtName != null) txtName.Text = item.Name;
                    if (txtPrice != null) txtPrice.Text = item.Price.ToString("0");
                    if (numStock != null) numStock.Value = Math.Clamp(item.StockQuantity, numStock.Minimum, numStock.Maximum);
                    if (txtSpecs != null) txtSpecs.Text = item.Specifications ?? "";
                    if (txtImageUrl != null) txtImageUrl.Text = item.ImageUrl ?? "";
                    LoadImagePreview(item.ImageUrl);
                }
            }
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            if (_operationInProgress || !ValidateInput(out decimal price)) return;
            var product = ReadProductInput(0, price);
            await RunProductOperationAsync(async () =>
            {
                // Không suy đoán mã danh mục từ dòng cũ hoặc mặc định bằng 1.
                var categoryId = await ChooseCategoryAsync(null);
                if (!categoryId.HasValue || IsDisposed) return;
                product.CategoryId = categoryId.Value;
                if (!await _apiService.AddProductAsync(product) || IsDisposed) return;
                ClearInputs();
                await LoadProductsAsync();
                if (!IsDisposed)
                    MessageBox.Show(this, "Thêm sản phẩm thành công!", "Thành công",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_operationInProgress) return;
            var current = _productList.FirstOrDefault(p => p.ProductId == _selectedProductId);
            if (current == null)
            {
                MessageBox.Show(this, "Vui lòng chọn sản phẩm cần cập nhật!", "Nhắc nhở",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ValidateInput(out decimal price)) return;
            var product = ReadProductInput(current.ProductId, price);
            int expectedStock = current.StockQuantity;
            await RunProductOperationAsync(async () =>
            {
                var categoryId = await ChooseCategoryAsync(current.CategoryId);
                if (!categoryId.HasValue || IsDisposed) return;
                product.CategoryId = categoryId.Value;
                if (await _apiService.UpdateProductAsync(product, expectedStock))
                {
                    if (IsDisposed) return;
                    ClearInputs();
                    await LoadProductsAsync();
                    if (!IsDisposed)
                        MessageBox.Show(this, "Cập nhật sản phẩm thành công!", "Thành công",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (_apiService.LastOrderStatusCode == 409 && !IsDisposed)
                {
                    ClearInputs();
                    await LoadProductsAsync();
                }
            });
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (_operationInProgress) return;
            var current = _productList.FirstOrDefault(p => p.ProductId == _selectedProductId);
            if (current == null)
            {
                MessageBox.Show(this, "Vui lòng chọn sản phẩm cần xóa!", "Nhắc nhở",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(this,
                $"Xóa sản phẩm #{current.ProductId}: {current.Name}?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            await RunProductOperationAsync(async () =>
            {
                if (!await _apiService.DeleteProductAsync(current.ProductId) || IsDisposed) return;
                ClearInputs();
                await LoadProductsAsync();
                if (!IsDisposed)
                    MessageBox.Show(this, "Xóa sản phẩm thành công!", "Thành công",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        private Product ReadProductInput(int id, decimal price) => new()
        {
            ProductId = id,
            Name = txtName.Text.Trim(),
            Price = price,
            StockQuantity = (int)numStock.Value,
            Specifications = txtSpecs.Text.Trim(),
            ImageUrl = txtImageUrl.Text.Trim()
        };

        private async Task RunProductOperationAsync(Func<Task> operation)
        {
            if (_operationInProgress || IsDisposed) return;
            _operationInProgress = true;
            // Khóa thao tác trong khi chờ API, khôi phục đúng trạng thái ban đầu.
            var controls = new Control[] { btnAdd, btnUpdate, btnDelete, btnReload,
                btnClear, btnViewDetail, btnOpenOrders, dgvProducts,
                txtName, txtPrice, numStock, txtSpecs, txtImageUrl, txtSearch };
            var states = controls.Select(c => (Control: c, Enabled: c.Enabled)).ToArray();
            foreach (var item in states) item.Control.Enabled = false;
            try { await operation(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                if (!IsDisposed)
                    MessageBox.Show(this,
                        "Không hoàn tất thao tác. Nếu yêu cầu đã gửi, hãy tải lại danh sách để kiểm tra trước khi thử lại.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _operationInProgress = false;
                if (!IsDisposed)
                    foreach (var item in states)
                        if (!item.Control.IsDisposed) item.Control.Enabled = item.Enabled;
            }
        }

        private async Task<int?> ChooseCategoryAsync(int? currentId)
        {
            // Lấy danh mục mới từ API mỗi lần lưu; lỗi tải không tự chọn thay.
            var categories = await _apiService.GetCategoriesAsync();
            if (IsDisposed || _apiService.LastError != null) return null;
            categories = categories.Where(c => c.CategoryId > 0)
                .OrderBy(c => c.Name).ToList();
            if (categories.Count == 0)
            {
                MessageBox.Show(this, "Chưa có danh mục. Hãy kiểm tra dữ liệu danh mục trước khi lưu.",
                    "Thiếu danh mục", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Hộp bổ sung trong cùng file; không chỉnh Designer/bố cục form chính.
            using var dialog = new Form
            {
                Text = "Chọn danh mục sản phẩm",
                ClientSize = new Size(430, 165),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.FromArgb(14, 21, 22),
                ForeColor = Color.White,
                AutoScaleMode = AutoScaleMode.Dpi
            };
            var label = new Label
            {
                Text = "Danh mục của sản phẩm:",
                AutoSize = true,
                Location = new Point(20, 20),
                ForeColor = Color.FromArgb(93, 242, 214)
            };
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 50),
                Size = new Size(390, 30),
                DisplayMember = nameof(Category.Name)
            };
            // Nạp mục trước khi chọn; ComboBox chưa nằm trong BindingContext của form.
            combo.Items.AddRange(categories.Cast<object>().ToArray());
            combo.SelectedIndex = currentId.HasValue
                ? categories.FindIndex(c => c.CategoryId == currentId.Value) : -1;
            var save = new Button
            {
                Text = "Xác nhận",
                Location = new Point(190, 110),
                Size = new Size(105, 35),
                BackColor = Color.FromArgb(0, 121, 107),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = combo.SelectedItem is Category
            };
            var cancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Location = new Point(305, 110),
                Size = new Size(105, 35),
                BackColor = Color.FromArgb(30, 41, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            combo.SelectedIndexChanged += (_, _) => save.Enabled = combo.SelectedItem is Category;
            save.Click += (_, _) =>
            {
                if (combo.SelectedItem is Category) dialog.DialogResult = DialogResult.OK;
            };
            dialog.Controls.AddRange(new Control[] { label, combo, save, cancel });
            dialog.AcceptButton = save;
            dialog.CancelButton = cancel;
            return dialog.ShowDialog(this) == DialogResult.OK && combo.SelectedItem is Category selected
                ? selected.CategoryId : null;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private async void btnReload_Click(object sender, EventArgs e)
        {
            await RunProductOperationAsync(LoadProductsAsync);
        }

        private void btnOpenOrders_Click(object sender, EventArgs e)
        {
            using var orderForm = new OrderManagementForm();
            orderForm.ShowDialog(this);
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            RenderGrid();
        }

        private void txtImageUrl_Leave(object sender, EventArgs e)
        {
            if (txtImageUrl != null)
            {
                LoadImagePreview(txtImageUrl.Text.Trim());
            }
        }

        private void LoadImagePreview(string? url)
        {
            if (picPreview == null) return;

            try
            {
                picPreview.CancelAsync();
                var imageUrl = ApiService.ResolveImageUrl(url);
                if (imageUrl is null) picPreview.Image = null;
                else picPreview.LoadAsync(imageUrl);
            }
            catch
            {
                picPreview.Image = null;
            }
        }

        private bool ValidateInput(out decimal price)
        {
            price = 0;
            if (txtName == null || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên máy tính!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName?.Focus();
                return false;
            }

            if (txtPrice == null || !decimal.TryParse(txtPrice.Text.Trim(), out price) || price < 0)
            {
                MessageBox.Show("Đơn giá không hợp lệ!", "Sai định dạng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrice?.Focus();
                return false;
            }

            return true;
        }

        private void ClearInputs()
        {
            _selectedProductId = 0;
            if (txtName != null) txtName.Text = "";
            if (txtPrice != null) txtPrice.Text = "";
            if (numStock != null) numStock.Value = 0;
            if (txtSpecs != null) txtSpecs.Text = "";
            if (txtImageUrl != null) txtImageUrl.Text = "";
            LoadImagePreview(null);
            txtName?.Focus();
        }

        private void txtSpecs_TextChanged(object sender, EventArgs e)
        {
        }

        private void picPreview_Click(object sender, EventArgs e)
        {
        }
    }
}
