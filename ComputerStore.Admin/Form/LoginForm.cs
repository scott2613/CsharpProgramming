using ComputerStore.Admin.Services;
namespace ComputerStore.Admin.Forms;
public partial class LoginForm : Form
{
    private readonly ApiService _api = new();
    private readonly TextBox _username = new() { Dock = DockStyle.Fill, PlaceholderText = "Tên đăng nhập, email hoặc số điện thoại" };
    private readonly TextBox _password = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true, PlaceholderText = "Mật khẩu" };
    private readonly Label _message = new() { AutoSize = false, Dock = DockStyle.Fill, ForeColor = Color.Salmon };
    private readonly Button _login = new() { Text = "Đăng nhập Admin", Dock = DockStyle.Fill, Height = 40 };
    public LoginForm()
    {
        InitializeComponent(); Controls.Clear(); Text = "ComputerStore - Đăng nhập quản trị";
        ClientSize = new Size(470, 320); StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        BackColor = Color.FromArgb(14, 21, 22); ForeColor = Color.White; Font = new Font("Segoe UI", 10F);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 6 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Chỉ tài khoản Admin được truy cập", Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(_username, 0, 1); layout.Controls.Add(_password, 0, 2); layout.Controls.Add(_message, 0, 3); layout.Controls.Add(_login, 0, 4);
        Controls.Add(layout); AcceptButton = _login; _login.Click += Login_Click;
    }
    private async void Login_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_username.Text) || string.IsNullOrWhiteSpace(_password.Text))
        { _message.Text = "Vui lòng nhập đầy đủ tài khoản và mật khẩu."; return; }
        _login.Enabled = false; _message.Text = "Đang xác thực...";
        try
        {
            var ok = await _api.LoginAsync(_username.Text, _password.Text);
            if (IsDisposed) return;
            if (ok) { _password.Clear(); DialogResult = DialogResult.OK; Close(); }
            else _message.Text = _api.LastError ?? "Đăng nhập thất bại.";
        }
        finally { if (!IsDisposed) _login.Enabled = true; }
    }
}
