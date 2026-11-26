using ComputerStore.Admin.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerStore.Admin
{
    public partial class UserManagementForm
    {
        private StableScrollPanel? _accountEditorScroll;
        internal void ResetEditorScroll()
        {
            if (_accountEditorScroll != null) _accountEditorScroll.AutoScrollPosition = Point.Empty;
        }

        private void SetupAccountLayout()
        {
            SuspendLayout();
            Controls.Clear();
            pnlHeader.Controls.Clear();
            pnlDetails.Controls.Clear();
            var page = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 3
            };
            page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            AdminLayout.Heading(lblTitle, "QUẢN LÝ TÀI KHOẢN");
            page.Controls.Add(lblTitle, 0, 0);
            lblSearch.Text = "Tìm tài khoản, họ tên, SĐT hoặc email";
            _reloadUsers.Margin = new Padding(0, 24, 0, 0);
            var tools = AdminLayout.Toolbar(AdminLayout.Field(lblSearch, txtSearch, 380), _reloadUsers);
            _reloadUsers.Margin = new Padding(0, 24, 0, 8);
            page.Controls.Add(tools, 0, 1);

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            dgvUsers.Dock = DockStyle.Fill;
            dgvUsers.Margin = new Padding(0, 0, 16, 0);
            body.Controls.Add(dgvUsers, 0, 0);
            pnlDetails.Dock = DockStyle.Fill;
            pnlDetails.Margin = Padding.Empty;
            pnlDetails.AutoScroll = false;
            pnlDetails.Padding = new Padding(12);
            var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            lblDetailTitle.Text = "THÔNG TIN TÀI KHOẢN";
            lblDetailTitle.AutoSize = false;
            lblDetailTitle.AutoEllipsis = true;
            lblDetailTitle.Dock = DockStyle.Fill;
            lblDetailTitle.Margin = Padding.Empty;
            editor.Controls.Add(lblDetailTitle, 0, 0);
            _accountEditorScroll = new StableScrollPanel();
            var fields = AdminLayout.Column();
            fields.BackColor = AdminTheme.Surface;
            AddField(lblUsername, txtUsername);
            AddField(lblFullName, txtFullName);
            AddField(lblPhone, txtPhone);
            AddField(lblEmail, txtEmail);
            AddField(lblRole, cboRole);
            var password = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Height = 40, Margin = Padding.Empty };
            password.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            password.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66));
            password.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            txtNewPassword.Dock = DockStyle.Fill;
            txtNewPassword.Margin = new Padding(0, 0, 6, 0);
            password.Controls.Add(txtNewPassword, 0, 0);
            AdminTheme.StyleButton(btnTogglePassword);
            btnTogglePassword.AutoSize = false;
            btnTogglePassword.MinimumSize = Size.Empty;
            btnTogglePassword.Dock = DockStyle.Fill;
            btnTogglePassword.Margin = Padding.Empty;
            password.Controls.Add(btnTogglePassword, 1, 0);
            password.Name = "AdminCompactAction";
            AddField(lblPassTitle, password);
            _accountEditorScroll.Controls.Add(fields);
            editor.Controls.Add(_accountEditorScroll, 0, 1);
            btnAddNew.Text = "Tạo mới"; btnSave.Text = "Cập nhật";
            btnResetPass.Text = "Cấp mật khẩu";
            btnDelete.Text = "Xóa tài khoản";
            btnViewOrders.Text = "Xem đơn hàng của tài khoản";
            editor.Controls.Add(AdminLayout.Actions(btnAddNew, btnSave, btnResetPass, btnDelete, btnViewOrders), 0, 2);
            pnlDetails.Controls.Add(editor);
            body.Controls.Add(pnlDetails, 1, 0);
            page.Controls.Add(body, 0, 2);
            Controls.Add(page);
            ResumeLayout(true);

            void AddField(Label label, Control input)
            {
                var field = AdminLayout.Field(label, input, 300);
                field.Dock = DockStyle.Fill;
                AdminLayout.Row(fields, field);
            }
        }
    }
}

namespace ComputerStore.Admin.Forms
{
    public partial class OrderManagementForm
    {
        private void SetupOrderLayout()
        {
            panelHeader.Controls.Clear();
            panelAction.Controls.Clear();
            panelHeader.AutoSize = true;
            panelHeader.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panelHeader.Padding = new Padding(20, 16, 20, 0);
            var header = AdminLayout.Column();
            AdminLayout.Heading(lblTitle, "QUẢN LÝ ĐƠN HÀNG");
            AdminLayout.Row(header, lblTitle);
            lblSearch.Text = "Tìm mã đơn, khách hàng, SĐT hoặc địa chỉ";
            lblFilter.Text = "Trạng thái đơn";
            btnReload.Text = "Làm mới";
            var filters = AdminLayout.Toolbar(
                AdminLayout.Field(lblSearch, txtSearchOrder, 350),
                AdminLayout.Field(lblFilter, cboStatusFilter, 200), btnReload);
            filters.WrapContents = false;
            filters.Margin = Padding.Empty;
            btnReload.Margin = new Padding(0, 24, 0, 8);
            lblChange.Text = "Chuyển trạng thái";
            btnSaveStatus.Text = "Lưu trạng thái";
            btnEditShipping.Text = "Sửa giao hàng";
            btnDeleteOrder.Text = "Xóa đơn hàng";
            var actions = AdminLayout.Toolbar(
                AdminLayout.Field(lblChange, cboNewStatus, 200),
                btnSaveStatus, btnEditShipping, btnDeleteOrder);
            actions.WrapContents = false;
            actions.Margin = Padding.Empty;
            foreach (var button in new[] { btnSaveStatus, btnEditShipping, btnDeleteOrder })
                button.Margin = new Padding(0, 24, 8, 8);
            btnDeleteOrder.Margin = new Padding(0, 24, 0, 8);

            // Một hàng duy nhất: bộ lọc trái, khoảng trống ở giữa, thao tác sát phải.
            // Nếu cửa sổ quá hẹp, chỉ thanh công cụ cuộn ngang; không đè hoặc xuống hàng.
            var viewport = new Panel { Dock = DockStyle.Top, AutoScroll = true, Height = 84, Margin = Padding.Empty };
            var toolbar = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 650));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 620));
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            filters.Dock = actions.Dock = DockStyle.Fill;
            toolbar.Controls.Add(filters, 0, 0);
            toolbar.Controls.Add(actions, 2, 0);
            viewport.Controls.Add(toolbar);
            AdminLayout.Row(header, viewport);
            panelHeader.Controls.Add(header);
            splitContainerMain.Panel2.Controls.Remove(panelAction);
            panelAction.Dispose();
            bool arranging = false;
            viewport.Layout += (_, _) => FitToolbar();
            Shown += (_, _) => FitToolbar();
            FitToolbar();

            void FitToolbar()
            {
                if (arranging || viewport.IsDisposed) return;
                arranging = true;
                try
                {
                    int left = NeededWidth(filters), right = NeededWidth(actions);
                    int height = Math.Max(NeededHeight(filters), NeededHeight(actions));
                    int minimum = left + right + 24;
                    bool overflow = viewport.ClientSize.Width < minimum;
                    toolbar.ColumnStyles[0].Width = left;
                    toolbar.ColumnStyles[2].Width = right;
                    toolbar.Size = new Size(Math.Max(viewport.ClientSize.Width, minimum), height);
                    viewport.Height = height + (overflow ? SystemInformation.HorizontalScrollBarHeight : 0) + 4;
                    viewport.AutoScrollMinSize = new Size(minimum, 0);
                }
                finally { arranging = false; }
            }
            static int NeededWidth(FlowLayoutPanel flow)
            {
                int width = 0;
                foreach (Control child in flow.Controls)
                    width += (child.AutoSize ? child.PreferredSize.Width : child.Width) + child.Margin.Horizontal;
                return width;
            }
            static int NeededHeight(FlowLayoutPanel flow)
            {
                int height = 0;
                foreach (Control child in flow.Controls)
                    height = System.Math.Max(height, (child.AutoSize ? child.PreferredSize.Height : child.Height) + child.Margin.Vertical);
                return height;
            }
            splitContainerMain.Panel1MinSize = 120;
            splitContainerMain.Panel2MinSize = 180;
        }
    }

    public partial class ProductManagementForm
    {
        private StableScrollPanel? _productEditorScroll;
        internal void ResetEditorScroll()
        {
            if (_productEditorScroll != null) _productEditorScroll.AutoScrollPosition = Point.Empty;
        }

        private void SetupProductLayout()
        {
            SuspendLayout();
            Controls.Clear();
            var page = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 3
            };
            page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            AdminLayout.Heading(lblTitle, "QUẢN LÝ SẢN PHẨM");
            page.Controls.Add(lblTitle, 0, 0);
            lblSearch.Text = "Tìm mã sản phẩm, tên hoặc cấu hình";
            var tools = AdminLayout.Toolbar(AdminLayout.Field(lblSearch, txtSearch, 380), btnReload);
            btnReload.Margin = new Padding(0, 24, 0, 8);
            page.Controls.Add(tools, 0, 1);
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            dgvProducts.Dock = DockStyle.Fill;
            dgvProducts.Margin = new Padding(0, 0, 16, 0);
            body.Controls.Add(dgvProducts, 0, 0);
            var editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12),
                BackColor = AdminTheme.Surface,
                Margin = Padding.Empty
            };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.Controls.Add(new Label { Text = "THÔNG TIN SẢN PHẨM", Dock = DockStyle.Fill, ForeColor = AdminTheme.Accent, AutoEllipsis = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 0);
            _productEditorScroll = new StableScrollPanel();
            var fields = AdminLayout.Column();
            fields.BackColor = AdminTheme.Surface;
            AddField(lblName, txtName);
            AddField(lblPrice, txtPrice);
            AddField(lblStock, numStock);
            txtSpecs.Height = 70;
            AddField(lblSpecs, txtSpecs);
            AddField(lblImageUrl, txtImageUrl);
            picPreview.MinimumSize = Size.Empty;
            picPreview.Height = 160;
            picPreview.Dock = DockStyle.Fill;
            picPreview.Margin = new Padding(0, 4, 12, 12);
            AdminLayout.Row(fields, picPreview);
            _productEditorScroll.Controls.Add(fields);
            editor.Controls.Add(_productEditorScroll, 0, 1);
            btnUpdate.Text = "Lưu thay đổi";
            editor.Controls.Add(AdminLayout.Actions(btnAdd, btnUpdate, btnDelete, btnClear, btnViewDetail, btnOpenOrders), 0, 2);
            body.Controls.Add(editor, 1, 0);
            page.Controls.Add(body, 0, 2);
            Controls.Add(page);
            ResumeLayout(true);

            void AddField(Label label, Control input)
            {
                var field = AdminLayout.Field(label, input, 320);
                field.Dock = DockStyle.Fill;
                AdminLayout.Row(fields, field);
            }
        }
    }
}
