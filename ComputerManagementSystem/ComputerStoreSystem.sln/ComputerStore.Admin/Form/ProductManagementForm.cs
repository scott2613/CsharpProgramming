using ComputerStore.Admin.Services;
using ComputerStore.Core.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerStore.Admin.Forms
{
    public partial class ProductManagementForm : Form
    {
        private readonly ApiService _apiService;
        private readonly HttpClient _httpClient;
        private List<Product> _productList = new();
        private int _selectedProductId = 0;
        private int? _selectedCategoryId = null;

        public ProductManagementForm()
        {
            InitializeComponent();
            SetupCyberGridStyle();

            _apiService = new ApiService();

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(ApiService.BaseUrl)
            };
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
            await LoadProductsAsync();
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
            if (_selectedProductId <= 0)
            {
                if (dgvProducts.CurrentRow != null && int.TryParse(dgvProducts.CurrentRow.Cells["Mã_SP"]?.Value?.ToString(), out int curId))
                {
                    _selectedProductId = curId;
                }
            }

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
                _productList = await _apiService.GetProductsAsync();
                RenderGrid();
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
                    _selectedCategoryId = item.CategoryId;
                    if (txtName != null) txtName.Text = item.Name;
                    if (txtPrice != null) txtPrice.Text = item.Price.ToString("0");
                    if (numStock != null) numStock.Value = Math.Max(0, item.StockQuantity);
                    if (txtSpecs != null) txtSpecs.Text = item.Specifications ?? "";
                    if (txtImageUrl != null) txtImageUrl.Text = item.ImageUrl ?? "";
                    LoadImagePreview(item.ImageUrl);
                }
            }
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput(out decimal price)) return;

            var product = new Product
            {
                ProductId = 0,
                Name = txtName?.Text.Trim() ?? "",
                Price = price,
                StockQuantity = (int)(numStock?.Value ?? 0),
                Specifications = txtSpecs?.Text.Trim() ?? "",
                ImageUrl = txtImageUrl?.Text.Trim() ?? "",
                CategoryId = _selectedCategoryId ?? 1
            };

            bool success = await _apiService.AddProductAsync(product);
            if (success)
            {
                MessageBox.Show("Thêm mới máy tính vào kho thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearInputs();
                await LoadProductsAsync();
            }
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedProductId <= 0)
            {
                MessageBox.Show("Vui lòng chọn một máy tính trong danh sách để cập nhật kho và thông tin!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateInput(out decimal price)) return;

            var currentItem = _productList.FirstOrDefault(p => p.ProductId == _selectedProductId);
            int categoryId = currentItem?.CategoryId ?? _selectedCategoryId ?? 1;

            var payload = new
            {
                ProductId = _selectedProductId,
                Name = txtName?.Text.Trim() ?? "",
                Price = price,
                StockQuantity = (int)(numStock?.Value ?? 0),
                Description = txtSpecs?.Text.Trim() ?? "",
                ImageUrl = txtImageUrl?.Text.Trim() ?? "",
                CategoryId = categoryId
            };

            try
            {
                var response = await _httpClient.PutAsJsonAsync($"products/{_selectedProductId}", payload);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Cập nhật thông tin & tồn kho cho máy #{_selectedProductId} thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadProductsAsync();
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi cập nhật kho (HTTP {(int)response.StatusCode}):\n{msg}", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedProductId <= 0)
            {
                MessageBox.Show("Vui lòng chọn máy tính cần xóa!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa máy tính #{_selectedProductId} ({txtName?.Text}) không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            bool success = await _apiService.DeleteProductAsync(_selectedProductId);
            if (success)
            {
                MessageBox.Show("Xóa sản phẩm thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearInputs();
                await LoadProductsAsync();
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private async void btnReload_Click(object sender, EventArgs e)
        {
            await LoadProductsAsync();
        }

        private void btnOpenOrders_Click(object sender, EventArgs e)
        {
            var orderForm = new OrderManagementForm();
            orderForm.ShowDialog();
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
                if (!string.IsNullOrWhiteSpace(url))
                {
                    picPreview.LoadAsync(url);
                }
                else
                {
                    picPreview.Image = null;
                }
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
            _selectedCategoryId = null;
            if (txtName != null) txtName.Text = "";
            if (txtPrice != null) txtPrice.Text = "";
            if (numStock != null) numStock.Value = 0;
            if (txtSpecs != null) txtSpecs.Text = "";
            if (txtImageUrl != null) txtImageUrl.Text = "";
            if (picPreview != null) picPreview.Image = null;
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