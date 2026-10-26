using ComputerStore.Admin.Forms;
using ComputerStore.Admin.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerStore.Admin
{
    public partial class UserManagementForm : Form
    {
        private readonly ApiService _apiService;
        private List<UserDto> _userList = new();
        private int _selectedUserId = 0;

        public UserManagementForm()
        {
            InitializeComponent();
            SetupGridStyle();
            _apiService = new ApiService();
        }

        private void SetupGridStyle()
        {
            dgvUsers.EnableHeadersVisualStyles = false;
            dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 61, 53);
            dgvUsers.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(93, 242, 214);
            dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvUsers.DefaultCellStyle.BackColor = Color.FromArgb(14, 21, 22);
            dgvUsers.DefaultCellStyle.ForeColor = Color.White;
            dgvUsers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 121, 107);
            dgvUsers.DefaultCellStyle.SelectionForeColor = Color.White;
        }

        private async void UserManagementForm_Load(object sender, EventArgs e)
        {
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            _userList = await _apiService.GetUsersAsync();
            FilterGrid();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            FilterGrid();
        }

        private void FilterGrid()
        {
            string kw = txtSearch.Text.Trim().ToLower();
            var list = string.IsNullOrEmpty(kw)
                ? _userList
                : _userList.Where(u =>
                    (u.Username != null && u.Username.ToLower().Contains(kw)) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(kw)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)) ||
                    (u.Email != null && u.Email.ToLower().Contains(kw))
                ).ToList();

            dgvUsers.DataSource = null;
            dgvUsers.DataSource = list.Select(u => new
            {
                Mã_User = u.UserId,
                Tên_Đăng_Nhập = u.Username,
                Họ_Tên = u.FullName,
                Số_Điện_Thoại = u.PhoneNumber,
                Email = u.Email,
                Vai_Trò = u.RoleName
            }).ToList();

            if (dgvUsers.Columns["Mã_User"] != null)
                dgvUsers.Columns["Mã_User"].Width = 80;
        }

        private void dgvUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvUsers.Rows.Count == 0) return;

            var row = dgvUsers.Rows[e.RowIndex];
            object? val = row.Cells["Mã_User"]?.Value ?? row.Cells[0]?.Value;
            if (val != null && int.TryParse(val.ToString(), out int id))
            {
                var user = _userList.FirstOrDefault(u => u.UserId == id);
                if (user != null)
                {
                    _selectedUserId = user.UserId;
                    txtUsername.Text = user.Username;
                    txtFullName.Text = user.FullName;
                    txtPhone.Text = user.PhoneNumber ?? "";
                    txtEmail.Text = user.Email ?? "";

                    if (cboRole.Items.Count > 0)
                    {
                        cboRole.SelectedIndex = user.RoleId == 1 ? 0 : 1;
                    }
                    txtNewPassword.Text = "";
                }
            }
        }

        private async void btnAddNew_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string pass = txtNewPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Vui lòng nhập Tên đăng nhập cho tài khoản mới!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(pass) || pass.Length < 6)
            {
                MessageBox.Show("Vui lòng nhập Mật khẩu (tối thiểu 6 ký tự) ở ô 'CẤP LẠI MẬT KHẨU MỚI'!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewPassword.Focus();
                return;
            }

            int roleId = (cboRole.SelectedItem != null && cboRole.SelectedItem.ToString()!.Contains("Admin")) ? 1 : 2;

            bool success = await _apiService.CreateUserAsync(username, pass, fullName, phone, email, roleId);
            if (success)
            {
                MessageBox.Show("Thêm mới tài khoản thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtNewPassword.Text = "";
                await LoadUsersAsync();
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (_selectedUserId <= 0 && dgvUsers.CurrentRow != null)
            {
                object? val = dgvUsers.CurrentRow.Cells["Mã_User"]?.Value ?? dgvUsers.CurrentRow.Cells[0]?.Value;
                if (val != null && int.TryParse(val.ToString(), out int curId))
                {
                    _selectedUserId = curId;
                }
            }

            if (_selectedUserId <= 0)
            {
                MessageBox.Show("Vui lòng click chọn một người dùng từ bảng danh sách trước khi cập nhật!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string newUsername = txtUsername.Text.Trim();
            if (string.IsNullOrWhiteSpace(newUsername))
            {
                MessageBox.Show("Tên đăng nhập không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            int roleId = (cboRole.SelectedItem != null && cboRole.SelectedItem.ToString()!.Contains("Admin")) ? 1 : 2;

            bool success = await _apiService.UpdateUserAsync(
                _selectedUserId,
                newUsername,
                txtFullName.Text.Trim(),
                txtPhone.Text.Trim(),
                txtEmail.Text.Trim(),
                roleId
            );

            if (success)
            {
                MessageBox.Show($"Cập nhật tài khoản #{_selectedUserId} thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadUsersAsync();
            }
        }

        private async void btnResetPass_Click(object sender, EventArgs e)
        {
            if (_selectedUserId <= 0 && dgvUsers.CurrentRow != null)
            {
                object? val = dgvUsers.CurrentRow.Cells["Mã_User"]?.Value ?? dgvUsers.CurrentRow.Cells[0]?.Value;
                if (val != null && int.TryParse(val.ToString(), out int curId))
                {
                    _selectedUserId = curId;
                }
            }

            if (_selectedUserId <= 0)
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần đặt lại mật khẩu!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string newPass = txtNewPassword.Text.Trim();
            if (string.IsNullOrEmpty(newPass) || newPass.Length < 6)
            {
                MessageBox.Show("Mật khẩu mới phải có tối thiểu 6 ký tự!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool success = await _apiService.ResetUserPasswordAsync(_selectedUserId, newPass);
            if (success)
            {
                MessageBox.Show($"Đặt lại mật khẩu cho tài khoản #{_selectedUserId} ({txtUsername.Text}) thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtNewPassword.Text = "";
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedUserId <= 0 && dgvUsers.CurrentRow != null)
            {
                object? val = dgvUsers.CurrentRow.Cells["Mã_User"]?.Value ?? dgvUsers.CurrentRow.Cells[0]?.Value;
                if (val != null && int.TryParse(val.ToString(), out int curId))
                {
                    _selectedUserId = curId;
                }
            }

            if (_selectedUserId <= 0)
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần xóa!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa tài khoản #{_selectedUserId} ({txtUsername.Text}) không?\nThao tác này không thể hoàn tác!",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes) return;

            bool success = await _apiService.DeleteUserAsync(_selectedUserId);
            if (success)
            {
                MessageBox.Show("Xóa tài khoản thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _selectedUserId = 0;
                txtUsername.Text = "";
                txtFullName.Text = "";
                txtPhone.Text = "";
                txtEmail.Text = "";
                txtNewPassword.Text = "";
                await LoadUsersAsync();
            }
        }

        private void btnViewOrders_Click(object sender, EventArgs e)
        {
            if (_selectedUserId <= 0 && dgvUsers.CurrentRow != null)
            {
                object? val = dgvUsers.CurrentRow.Cells["Mã_User"]?.Value ?? dgvUsers.CurrentRow.Cells[0]?.Value;
                if (val != null && int.TryParse(val.ToString(), out int curId))
                {
                    _selectedUserId = curId;
                }
            }

            if (_selectedUserId <= 0)
            {
                MessageBox.Show("Vui lòng chọn một người dùng từ bảng danh sách để lọc đơn hàng!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string filterKey = !string.IsNullOrEmpty(txtPhone.Text.Trim())
                ? txtPhone.Text.Trim()
                : (!string.IsNullOrEmpty(txtFullName.Text.Trim()) ? txtFullName.Text.Trim() : txtUsername.Text.Trim());

            var orderForm = new OrderManagementForm(filterKey);
            orderForm.ShowDialog();
        }
    }
}