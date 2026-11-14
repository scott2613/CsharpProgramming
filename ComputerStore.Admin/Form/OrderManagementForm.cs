using ComputerStore.Admin.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerStore.Admin.Forms
{
    public partial class OrderManagementForm : Form
    {
        private readonly ApiService _apiService;
        private List<OrderDto> _orders = new();
        private readonly string? _initialCustomerFilter;
        private readonly CancellationTokenSource _requests = new();
        private readonly CancellationToken _requestToken;
        private bool _ready, _rendering, _busy, _closed, _dataFresh;
        private static readonly string[] Statuses =
            { "Chờ xử lý", "Đã xác nhận", "Đang giao", "Đã hoàn thành", "Đã hủy" };

        public OrderManagementForm() : this(null) { }
        public OrderManagementForm(string? initialCustomerFilter)
        {
            InitializeComponent();
            _apiService = new ApiService();
            _initialCustomerFilter = initialCustomerFilter?.Trim();
            _requestToken = _requests.Token;
            cboNewStatus.SelectedIndexChanged += (_, _) => UpdateActionState();
            FormClosed += (_, _) => { _closed = true; _requests.Cancel(); };
            Disposed += (_, _) => { _closed = true; _requests.Cancel(); _requests.Dispose(); };
        }
        private static string NormalizeStatus(string? status) => status?.Trim() switch
        {
            "Hoàn thành" => "Đã hoàn thành",
            "Đang giao hàng" => "Đang giao",
            var x => x ?? ""
        };
        private static bool IsActive(string status) => NormalizeStatus(status) is "Chờ xử lý" or "Đã xác nhận" or "Đang giao";
        private static bool CanMove(string current, string next)
        {
            current = NormalizeStatus(current); next = NormalizeStatus(next);
            return current == next || (current switch
            {
                "Chờ xử lý" => next is "Đã xác nhận" or "Đang giao" or "Đã hoàn thành" or "Đã hủy",
                "Đã xác nhận" => next is "Đang giao" or "Đã hoàn thành" or "Đã hủy",
                "Đang giao" => next is "Đã hoàn thành" or "Đã hủy",
                _ => false
            });
        }
        private async void OrderManagementForm_Load(object sender, EventArgs e)
        {
            ApplyCyberDarkTheme();
            _rendering = true;
            try
            {
                cboStatusFilter.Items.Clear();
                cboStatusFilter.Items.Add("Tất cả");
                cboStatusFilter.Items.AddRange(Statuses);
                cboStatusFilter.SelectedIndex = 0;
                cboNewStatus.Items.Clear();
            }
            finally { _rendering = false; }
            _ready = true;
            if (!string.IsNullOrEmpty(_initialCustomerFilter))
            {
                Text = $"DTBStore - Đơn Hàng Của Khách: {_initialCustomerFilter}";
                txtSearchOrder.Text = _initialCustomerFilter;
            }
            UpdateActionState();
            await FetchOrdersAsync();
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
        private OrderDto? SelectedOrder()
        {
            if (!dgvOrders.Columns.Contains("Mã_Đơn") || dgvOrders.CurrentRow == null) return null;
            var value = dgvOrders.CurrentRow.Cells["Mã_Đơn"].Value;
            return int.TryParse(value?.ToString(), out var id) ? _orders.FirstOrDefault(o => o.OrderId == id) : null;
        }
        private void UpdateActionState()
        {
            if (!_ready || _closed || IsDisposed) return;
            var order = SelectedOrder();
            bool canUse = !_busy && _dataFresh && order != null;
            bool active = order != null && IsActive(order.Status);
            cboNewStatus.Enabled = canUse && active;
            btnSaveStatus.Enabled = canUse && active && cboNewStatus.SelectedItem != null &&
                NormalizeStatus(order!.Status) != cboNewStatus.SelectedItem.ToString();
            btnEditShipping.Enabled = canUse && active;
            btnDeleteOrder.Enabled = canUse && order != null &&
                (active || NormalizeStatus(order.Status) == "Đã hủy");
            btnReload.Enabled = !_busy;
        }
        private void SetBusy(bool busy)
        {
            _busy = busy;
            if (_closed || IsDisposed) return;
            txtSearchOrder.Enabled = !busy;
            cboStatusFilter.Enabled = !busy;
            dgvOrders.Enabled = !busy;
            UseWaitCursor = busy;
            UpdateActionState();
        }
        private void Notify(string message, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            if (!_closed && !IsDisposed) MessageBox.Show(this, message, "Quản lý đơn hàng", MessageBoxButtons.OK, icon);
        }
        private void NotifyApiError()
        {
            Notify(_apiService.LastError ?? "Không hoàn tất được yêu cầu. Hãy tải lại danh sách.", MessageBoxIcon.Warning);
        }
        private async Task RunAsync(Func<Task> work)
        {
            if (_busy || _closed) return;
            SetBusy(true);
            try { await work(); }
            catch (OperationCanceledException) when (_requestToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                _dataFresh = false;
                Notify("Không hoàn tất thao tác. Hãy tải lại dữ liệu trước khi tiếp tục.", MessageBoxIcon.Error);
            }
            finally { SetBusy(false); }
        }
        private Task FetchOrdersAsync() => RunAsync(() => LoadOrdersCoreAsync(SelectedOrder()?.OrderId));
        private async Task LoadOrdersCoreAsync(int? keepId)
        {
            var loaded = await _apiService.GetOrdersAsync(_requestToken);
            if (_closed || IsDisposed) return;
            if (_apiService.LastError != null)
            {
                // Giữ danh sách đang xem, nhưng khóa thao tác vì chưa tải lại thành công.
                _dataFresh = false;
                NotifyApiError();
                return;
            }
            _orders = loaded;
            _dataFresh = true;
            RenderOrders(keepId);
        }
        private void RenderOrders(int? keepId = null)
        {
            if (!_ready || _rendering || _closed) return;
            keepId ??= SelectedOrder()?.OrderId;
            var filter = cboStatusFilter.SelectedItem?.ToString() ?? "Tất cả";
            var query = _orders.AsEnumerable();
            if (filter != "Tất cả") query = query.Where(o => NormalizeStatus(o.Status) == filter);
            var keyword = txtSearchOrder.Text.Trim();
            if (keyword.Length > 0) query = query.Where(o =>
                (o.CustomerName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (o.CustomerPhone ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (o.FinalAddress ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                o.OrderId.ToString().Contains(keyword));
            var displayList = query.OrderByDescending(o => o.OrderId).ToList();
            _rendering = true;
            try
            {
                dgvOrders.DataSource = null;
                dgvOrders.DataSource = displayList.Select(o => new
                {
                    Mã_Đơn = o.OrderId,
                    Khách_Hàng = !string.IsNullOrWhiteSpace(o.CustomerName) ? o.CustomerName : "Khách vãng lai",
                    Số_Điện_Thoại = !string.IsNullOrWhiteSpace(o.CustomerPhone) ? o.CustomerPhone : "Chưa có",
                    Địa_Chỉ = !string.IsNullOrWhiteSpace(o.FinalAddress) ? o.FinalAddress : "Chưa cập nhật địa chỉ",
                    Ngày_Đặt = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                    Tổng_Tiền = o.TotalAmount.ToString("N0") + " đ",
                    Trạng_Thái = NormalizeStatus(o.Status)
                }).ToList();
                SetColumnWeight(dgvOrders, "Mã_Đơn", 45);
                SetColumnWeight(dgvOrders, "Khách_Hàng", 90);
                SetColumnWeight(dgvOrders, "Số_Điện_Thoại", 85);
                SetColumnWeight(dgvOrders, "Địa_Chỉ", 140);
                SetColumnWeight(dgvOrders, "Ngày_Đặt", 80);
                SetColumnWeight(dgvOrders, "Tổng_Tiền", 80);
                SetColumnWeight(dgvOrders, "Trạng_Thái", 70);
                dgvOrders.ClearSelection();
                if (displayList.Count > 0)
                {
                    var index = displayList.FindIndex(o => o.OrderId == keepId);
                    if (index < 0) index = 0;
                    dgvOrders.CurrentCell = dgvOrders.Rows[index].Cells["Mã_Đơn"];
                    dgvOrders.Rows[index].Selected = true;
                }
            }
            finally { _rendering = false; }
            SyncSelection();
        }
        private void SyncSelection()
        {
            if (_rendering || !_ready || _closed) return;
            var order = SelectedOrder();
            if (order == null)
            {
                dgvDetails.DataSource = null;
                lblDetailsHeader.Text = "CHI TIẾT MẶT HÀNG TRONG ĐƠN:";
                cboNewStatus.Items.Clear();
                UpdateActionState();
                return;
            }
            DisplayDetails(order);
            var current = NormalizeStatus(order.Status);
            cboNewStatus.Items.Clear();
            cboNewStatus.Items.Add(current);
            if (IsActive(current))
                foreach (var status in Statuses.Where(s => s != current && CanMove(current, s)))
                    cboNewStatus.Items.Add(status);
            cboNewStatus.SelectedIndex = 0;
            UpdateActionState();
        }
        private void DisplayDetails(OrderDto order)
        {
            lblDetailsHeader.Text = $"CHI TIẾT MẶT HÀNG - ĐƠN HÀNG #{order.OrderId} ({order.CustomerName})";
            dgvDetails.DataSource = null;
            if (order.OrderDetails == null || order.OrderDetails.Count == 0) return;
            dgvDetails.DataSource = order.OrderDetails.Select(d => new
            {
                Mã_SP = d.ProductId,
                Tên_Sản_Phẩm = string.IsNullOrEmpty(d.ProductName) ? $"Sản phẩm #{d.ProductId}" : d.ProductName,
                Số_Lượng = d.Quantity,
                Đơn_Giá = d.UnitPrice.ToString("N0") + " đ",
                Thành_Tiền = (d.UnitPrice * d.Quantity).ToString("N0") + " đ"
            }).ToList();
            SetColumnWeight(dgvDetails, "Mã_SP", 40);
            SetColumnWeight(dgvDetails, "Tên_Sản_Phẩm", 200);
        }
        private static void SetColumnWeight(DataGridView grid, string name, float weight)
        {
            if (grid.Columns[name] is DataGridViewColumn column) column.FillWeight = weight;
        }

        private async Task<OrderDto?> LatestAsync(int id)
        {
            var order = await _apiService.GetOrderAsync(id, _requestToken);
            if (_closed) return null;
            if (order == null)
            {
                _dataFresh = false;
                NotifyApiError();
                return null;
            }
            var index = _orders.FindIndex(x => x.OrderId == id);
            if (index >= 0) _orders[index] = order;
            RenderOrders(id);
            return order;
        }
        private async Task AfterWriteAsync(bool success, int id, string message)
        {
            if (_closed) return;
            if (success) Notify(message);
            else NotifyApiError();
            // GET để đối chiếu kết quả; không tự gửi lại PUT/DELETE khi lỗi mạng.
            await LoadOrdersCoreAsync(id);
        }
        private async void btnSaveStatus_Click(object sender, EventArgs e)
        {
            if (!_dataFresh || _busy) return;
            var selected = SelectedOrder();
            var target = cboNewStatus.SelectedItem?.ToString();
            if (selected == null || target == null) { Notify("Vui lòng chọn đơn và trạng thái cần cập nhật."); return; }
            var id = selected.OrderId;
            await RunAsync(async () =>
            {
                var order = await LatestAsync(id);
                if (order == null) return;
                var current = NormalizeStatus(order.Status);
                if (current == target) { Notify("Đơn hàng đã ở trạng thái này."); return; }
                if (!CanMove(current, target)) { Notify("Đơn hàng đã thay đổi. Trạng thái mới không còn phù hợp; hãy kiểm tra lại.", MessageBoxIcon.Warning); return; }
                var extra = target == "Đã hủy" ? "\nHệ thống sẽ hoàn kho một lần; đơn đã hủy không được mở lại." :
                    target == "Đã hoàn thành" ? "\nĐơn hoàn thành sẽ được giữ làm lịch sử và không mở lại." : "";
                if (MessageBox.Show(this, $"Chuyển đơn #{id} từ '{current}' sang '{target}'?" + extra,
                    "Xác nhận cập nhật", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                var success = await _apiService.UpdateOrderStatusAsync(id, target, _requestToken);
                await AfterWriteAsync(success, id, $"Đã cập nhật đơn #{id} thành '{target}'.");
            });
        }
        private async void btnEditShipping_Click(object sender, EventArgs e)
        {
            if (!_dataFresh || _busy) return;
            var selected = SelectedOrder();
            if (selected == null) { Notify("Vui lòng chọn đơn cần sửa thông tin nhận hàng."); return; }
            var orderId = selected.OrderId;
            await RunAsync(async () =>
            {
                var order = await LatestAsync(orderId);
                if (order == null) return;
                if (!IsActive(order.Status)) { Notify("Không sửa thông tin nhận hàng của đơn đã kết thúc.", MessageBoxIcon.Warning); return; }
                // Giữ nguyên vị trí, kích thước, màu sắc hộp thoại cũ.
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
                TextBox txtName = new TextBox { Left = 20, Top = 45, Width = 400, MaxLength = 100, Text = order.CustomerName, BackColor = Color.FromArgb(20, 31, 33), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

                Label lblPhone = new Label { Left = 20, Top = 80, Text = "Số điện thoại:", AutoSize = true, ForeColor = Color.FromArgb(93, 242, 214) };
                TextBox txtPhone = new TextBox { Left = 20, Top = 105, Width = 400, MaxLength = 20, Text = order.CustomerPhone, BackColor = Color.FromArgb(20, 31, 33), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

                Label lblAddress = new Label { Left = 20, Top = 140, Text = "Địa chỉ nhận hàng:", AutoSize = true, ForeColor = Color.FromArgb(93, 242, 214) };
                TextBox txtAddress = new TextBox { Left = 20, Top = 165, Width = 400, MaxLength = 500, Text = order.FinalAddress ?? order.DeliveryAddress ?? "", BackColor = Color.FromArgb(20, 31, 33), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

                Button btnConfirm = new Button
                {
                    Text = "Lưu thay đổi",
                    Left = 280,
                    Width = 140,
                    Top = 208,
                    Height = 35,
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
                // Kiểm tra ngay trong hộp thoại, không đóng rồi làm mất nội dung đã nhập.
                btnConfirm.Click += (_, _) =>
                {
                    string name = txtName.Text.Trim(), phone = txtPhone.Text.Trim(), address = txtAddress.Text.Trim();
                    if (name.Length is < 1 or > 100) { MessageBox.Show(prompt, "Họ tên phải có từ 1 đến 100 ký tự."); txtName.Focus(); return; }
                    if (phone.Length is < 1 or > 20 || !new PhoneAttribute().IsValid(phone))
                    { MessageBox.Show(prompt, "Số điện thoại không hợp lệ (tối đa 20 ký tự)."); txtPhone.Focus(); return; }
                    if (address.Length is < 1 or > 500) { MessageBox.Show(prompt, "Địa chỉ phải có từ 1 đến 500 ký tự."); txtAddress.Focus(); return; }
                    prompt.DialogResult = DialogResult.OK;
                };
                if (prompt.ShowDialog(this) != DialogResult.OK || _closed) return;
                var success = await _apiService.UpdateOrderShippingAsync(orderId,
                    txtName.Text.Trim(), txtPhone.Text.Trim(), txtAddress.Text.Trim(), _requestToken);
                await AfterWriteAsync(success, orderId, $"Đã cập nhật thông tin nhận hàng đơn #{orderId}.");
            });
        }
        private async void btnDeleteOrder_Click(object sender, EventArgs e)
        {
            if (!_dataFresh || _busy) return;
            var selected = SelectedOrder();
            if (selected == null) { Notify("Vui lòng chọn đơn cần xóa."); return; }
            var id = selected.OrderId;
            await RunAsync(async () =>
            {
                var order = await LatestAsync(id);
                if (order == null) return;
                var status = NormalizeStatus(order.Status);
                if (status == "Đã hoàn thành") { Notify("Giữ đơn đã hoàn thành để đối chiếu lịch sử bán hàng."); return; }
                if (!IsActive(status) && status != "Đã hủy") { Notify("Trạng thái cũ chưa được nhận diện. Cần kiểm tra dữ liệu trước khi xóa."); return; }
                var extra = status == "Đã hủy" ? "Đơn đã hủy nên không hoàn kho thêm lần nữa." :
                    "Hệ thống sẽ hoàn trả số lượng máy tính trong đơn về kho.";
                if (MessageBox.Show(this, $"Bạn có chắc muốn xóa vĩnh viễn đơn #{id}?\n" + extra,
                    "Xác nhận xóa đơn hàng", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                var success = await _apiService.DeleteOrderAsync(id, _requestToken);
                await AfterWriteAsync(success, id, $"Đã xóa đơn hàng #{id}.");
            });
        }
        private void txtSearchOrder_TextChanged(object sender, EventArgs e) => RenderOrders();
        private void cboStatusFilter_SelectedIndexChanged(object sender, EventArgs e) => RenderOrders();
        private void dgvOrders_SelectionChanged(object sender, EventArgs e) => SyncSelection();
        private async void btnReload_Click(object sender, EventArgs e) => await FetchOrdersAsync();
        private void dgvOrders_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    }
}
