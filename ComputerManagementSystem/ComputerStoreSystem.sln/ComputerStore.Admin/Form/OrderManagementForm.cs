using ComputerStore.Admin.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerStore.Admin.Forms
{
    public partial class OrderManagementForm : Form
    {
        private readonly ApiService _apiService;
        private List<OrderDto> _orders = new();
        private readonly string[] _allStatuses = { "Tất cả", "Chờ xử lý", "Đang giao", "Đã hoàn thành", "Đã hủy" };
        private readonly string? _initialCustomerFilter;

        public OrderManagementForm() : this(null)
        {
        }

        public OrderManagementForm(string? initialCustomerFilter)
        {
            InitializeComponent();
            _apiService = new ApiService();
            _initialCustomerFilter = initialCustomerFilter?.Trim();
        }

        private async void OrderManagementForm_Load(object sender, EventArgs e)
        {
            ApplyCyberDarkTheme();
            InitFilterControls();

            if (!string.IsNullOrEmpty(_initialCustomerFilter))
            {
                this.Text = $"DTBStore - Đơn Hàng Của Khách: {_initialCustomerFilter}";
                txtSearchOrder.Text = _initialCustomerFilter;
            }

            await FetchOrdersAsync();
        }

        private void InitFilterControls()
        {
            cboStatusFilter.Items.Clear();
            cboStatusFilter.Items.AddRange(_allStatuses);
            cboStatusFilter.SelectedIndex = 0;

            cboNewStatus.Items.Clear();
            cboNewStatus.Items.AddRange(new string[] { "Chờ xử lý", "Đang giao", "Đã hoàn thành", "Đã hủy" });
            cboNewStatus.SelectedIndex = 0;
        }

        private void ApplyCyberDarkTheme()
        {
            void FormatGrid(DataGridView g)
            {
                g.EnableHeadersVisualStyles = false;
                g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 61, 53);
                g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(93, 242, 214);
                g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                g.ColumnHeadersHeight = 36;
                g.DefaultCellStyle.BackColor = Color.FromArgb(14, 21, 22);
                g.DefaultCellStyle.ForeColor = Color.White;
                g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 121, 107);
                g.DefaultCellStyle.SelectionForeColor = Color.White;
                g.BorderStyle = BorderStyle.None;
                g.GridColor = Color.FromArgb(30, 69, 62);
                g.RowHeadersVisible = false;
                g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                g.MultiSelect = false;
                g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            FormatGrid(dgvOrders);
            FormatGrid(dgvDetails);
        }

        private async Task FetchOrdersAsync()
        {
            try
            {
                _orders = await _apiService.GetOrdersAsync();
                RenderOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối API: {ex.Message}\nVui lòng kiểm tra ComputerStore.API đã chạy chưa.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderOrders()
        {
            string selectedStatus = cboStatusFilter.SelectedItem?.ToString() ?? "Tất cả";
            var query = _orders.AsEnumerable();

            if (selectedStatus != "Tất cả")
            {
                query = query.Where(o => string.Equals(o.Status, selectedStatus, StringComparison.OrdinalIgnoreCase));
            }

            string keyword = txtSearchOrder.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(o =>
                    (o.CustomerName != null && o.CustomerName.ToLower().Contains(keyword)) ||
                    (o.CustomerPhone != null && o.CustomerPhone.ToLower().Contains(keyword)) ||
                    (o.FinalAddress != null && o.FinalAddress.ToLower().Contains(keyword)) ||
                    o.OrderId.ToString().Contains(keyword)
                );
            }

            var displayList = query.ToList();

            dgvOrders.DataSource = null;
            dgvOrders.DataSource = displayList.Select(o => new
            {
                Mã_Đơn = o.OrderId,
                Khách_Hàng = !string.IsNullOrWhiteSpace(o.CustomerName) ? o.CustomerName : "Khách vãng lai",
                Số_Điện_Thoại = !string.IsNullOrWhiteSpace(o.CustomerPhone) ? o.CustomerPhone : "Chưa có",
                Địa_Chỉ = !string.IsNullOrWhiteSpace(o.FinalAddress) ? o.FinalAddress : (!string.IsNullOrWhiteSpace(o.DeliveryAddress) ? o.DeliveryAddress : "Chưa cập nhật địa chỉ"),
                Ngày_Đặt = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                Tổng_Tiền = o.TotalAmount.ToString("N0") + " đ",
                Trạng_Thái = o.Status
            }).ToList();

            if (dgvOrders.Columns["Mã_Đơn"] != null) dgvOrders.Columns["Mã_Đơn"].FillWeight = 45;
            if (dgvOrders.Columns["Khách_Hàng"] != null) dgvOrders.Columns["Khách_Hàng"].FillWeight = 90;
            if (dgvOrders.Columns["Số_Điện_Thoại"] != null) dgvOrders.Columns["Số_Điện_Thoại"].FillWeight = 85;
            if (dgvOrders.Columns["Địa_Chỉ"] != null) dgvOrders.Columns["Địa_Chỉ"].FillWeight = 140;
            if (dgvOrders.Columns["Ngày_Đặt"] != null) dgvOrders.Columns["Ngày_Đặt"].FillWeight = 80;
            if (dgvOrders.Columns["Tổng_Tiền"] != null) dgvOrders.Columns["Tổng_Tiền"].FillWeight = 80;
            if (dgvOrders.Columns["Trạng_Thái"] != null) dgvOrders.Columns["Trạng_Thái"].FillWeight = 70;

            if (dgvOrders.Rows.Count > 0)
            {
                dgvOrders.Rows[0].Selected = true;
                DisplayDetails(displayList.First());
            }
            else
            {
                dgvDetails.DataSource = null;
                lblDetailsHeader.Text = "CHI TIẾT MẶT HÀNG TRONG ĐƠN:";
            }
        }

        private void txtSearchOrder_TextChanged(object sender, EventArgs e)
        {
            RenderOrders();
        }

        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;

            object? val = dgvOrders.CurrentRow.Cells["Mã_Đơn"]?.Value;
            if (val != null && int.TryParse(val.ToString(), out int orderId))
            {
                var order = _orders.FirstOrDefault(x => x.OrderId == orderId);
                if (order != null)
                {
                    DisplayDetails(order);
                    if (cboNewStatus.Items.Contains(order.Status))
                    {
                        cboNewStatus.SelectedItem = order.Status;
                    }
                }
            }
        }

        private void DisplayDetails(OrderDto order)
        {
            lblDetailsHeader.Text = $"CHI TIẾT MẶT HÀNG - ĐƠN HÀNG #{order.OrderId} ({order.CustomerName})";

            dgvDetails.DataSource = null;
            if (order.OrderDetails != null && order.OrderDetails.Any())
            {
                dgvDetails.DataSource = order.OrderDetails.Select(d => new
                {
                    Mã_SP = d.ProductId,
                    Tên_Sản_Phẩm = string.IsNullOrEmpty(d.ProductName) ? $"Sản phẩm #{d.ProductId}" : d.ProductName,
                    Số_Lượng = d.Quantity,
                    Đơn_Giá = d.UnitPrice.ToString("N0") + " đ",
                    Thành_Tiền = d.TotalPrice.ToString("N0") + " đ"
                }).ToList();

                if (dgvDetails.Columns["Mã_SP"] != null) dgvDetails.Columns["Mã_SP"].FillWeight = 40;
                if (dgvDetails.Columns["Tên_Sản_Phẩm"] != null) dgvDetails.Columns["Tên_Sản_Phẩm"].FillWeight = 200;
            }
        }

        // CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG
        private async void btnSaveStatus_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn đơn hàng cần cập nhật!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            object? val = dgvOrders.CurrentRow.Cells["Mã_Đơn"]?.Value;
            if (val == null || !int.TryParse(val.ToString(), out int orderId))
            {
                return;
            }

            string newStatus = cboNewStatus.SelectedItem?.ToString() ?? "Chờ xử lý";

            bool success = await _apiService.UpdateOrderStatusAsync(orderId, newStatus);
            if (success)
            {
                MessageBox.Show($"Cập nhật đơn hàng #{orderId} thành '{newStatus}' thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await FetchOrdersAsync();
            }
        }

        // SỬA THÔNG TIN NHẬN HÀNG (HỌ TÊN, SĐT, ĐỊA CHỈ)
        private async void btnEditShipping_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn đơn hàng cần sửa thông tin nhận hàng!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["Mã_Đơn"].Value);
            var order = _orders.FirstOrDefault(x => x.OrderId == orderId);
            if (order == null) return;

            // Hộp thoại nhập thông tin nhanh
            using Form prompt = new Form
            {
                Width = 460,
                Height = 300,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = $"Cập nhật thông tin nhận hàng - Đơn #{orderId}",
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(14, 21, 22),
                ForeColor = Color.White
            };

            Label lblName = new Label { Left = 20, Top = 20, Text = "Họ tên người nhận:", AutoSize = true, ForeColor = Color.FromArgb(93, 242, 214) };
            TextBox txtName = new TextBox { Left = 20, Top = 45, Width = 400, Text = order.CustomerName, BackColor = Color.FromArgb(20, 31, 33), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            Label lblPhone = new Label { Left = 20, Top = 80, Text = "Số điện thoại:", AutoSize = true, ForeColor = Color.FromArgb(93, 242, 214) };
            TextBox txtPhone = new TextBox { Left = 20, Top = 105, Width = 400, Text = order.CustomerPhone, BackColor = Color.FromArgb(20, 31, 33), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            Label lblAddress = new Label { Left = 20, Top = 140, Text = "Địa chỉ nhận hàng:", AutoSize = true, ForeColor = Color.FromArgb(93, 242, 214) };
            TextBox txtAddress = new TextBox { Left = 20, Top = 165, Width = 400, Text = order.FinalAddress ?? order.DeliveryAddress ?? "", BackColor = Color.FromArgb(20, 31, 33), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            Button btnConfirm = new Button
            {
                Text = "Lưu thay đổi",
                Left = 280,
                Width = 140,
                Top = 208,
                Height = 35,
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 121, 107),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };

            prompt.Controls.Add(lblName);
            prompt.Controls.Add(txtName);
            prompt.Controls.Add(lblPhone);
            prompt.Controls.Add(txtPhone);
            prompt.Controls.Add(lblAddress);
            prompt.Controls.Add(txtAddress);
            prompt.Controls.Add(btnConfirm);
            prompt.AcceptButton = btnConfirm;

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                string newName = txtName.Text.Trim();
                string newPhone = txtPhone.Text.Trim();
                string newAddr = txtAddress.Text.Trim();

                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("Tên người nhận không được để trống!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool success = await _apiService.UpdateOrderShippingAsync(orderId, newName, newPhone, newAddr);
                if (success)
                {
                    MessageBox.Show($"Đã cập nhật thông tin nhận hàng đơn #{orderId} thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await FetchOrdersAsync();
                }
            }
        }

        // XÓA ĐƠN HÀNG VÀ TỰ ĐỘNG HOÀN TRẢ TỒN KHO
        private async void btnDeleteOrder_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn đơn hàng cần xóa!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["Mã_Đơn"].Value);
            string status = dgvOrders.CurrentRow.Cells["Trạng_Thái"].Value?.ToString() ?? "";

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa vĩnh viễn đơn hàng #{orderId} không?\n" +
                (status != "Đã hủy" && status != "Đã hoàn thành" ? "⚠️ Đơn này chưa hoàn thành, hệ thống sẽ tự động hoàn trả số lượng linh kiện về kho." : ""),
                "Xác nhận xóa đơn hàng",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes) return;

            bool success = await _apiService.DeleteOrderAsync(orderId);
            if (success)
            {
                MessageBox.Show($"Đã xóa đơn hàng #{orderId} thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await FetchOrdersAsync();
            }
        }

        private void cboStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            RenderOrders();
        }

        private async void btnReload_Click(object sender, EventArgs e)
        {
            await FetchOrdersAsync();
        }

        private void dgvOrders_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }
    }
}