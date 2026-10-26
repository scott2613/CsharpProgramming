namespace ComputerStore.Admin
{
    partial class UserManagementForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            pnlHeader = new Panel();
            txtSearch = new TextBox();
            lblSearch = new Label();
            lblTitle = new Label();
            pnlDetails = new Panel();
            btnViewOrders = new Button();
            btnDelete = new Button();
            btnSave = new Button();
            btnAddNew = new Button();
            btnResetPass = new Button();
            txtNewPassword = new TextBox();
            lblPassTitle = new Label();
            cboRole = new ComboBox();
            lblRole = new Label();
            txtEmail = new TextBox();
            lblEmail = new Label();
            txtPhone = new TextBox();
            lblPhone = new Label();
            txtFullName = new TextBox();
            lblFullName = new Label();
            txtUsername = new TextBox();
            lblUsername = new Label();
            lblDetailTitle = new Label();
            dgvUsers = new DataGridView();
            pnlHeader.SuspendLayout();
            pnlDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvUsers).BeginInit();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(14, 21, 22);
            pnlHeader.Controls.Add(txtSearch);
            pnlHeader.Controls.Add(lblSearch);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new Padding(20, 10, 20, 10);
            pnlHeader.Size = new Size(1150, 60);
            pnlHeader.TabIndex = 0;
            // 
            // txtSearch
            // 
            txtSearch.BackColor = Color.FromArgb(20, 31, 33);
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.Dock = DockStyle.Right;
            txtSearch.Font = new Font("Segoe UI", 10F);
            txtSearch.ForeColor = Color.White;
            txtSearch.Location = new Point(828, 10);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Nhập tên, tài khoản, SĐT...";
            txtSearch.Size = new Size(220, 30);
            txtSearch.TabIndex = 2;
            txtSearch.TextChanged += txtSearch_TextChanged;
            // 
            // lblSearch
            // 
            lblSearch.AutoSize = true;
            lblSearch.Dock = DockStyle.Right;
            lblSearch.Font = new Font("Segoe UI", 9.5F);
            lblSearch.ForeColor = Color.FromArgb(141, 164, 164);
            lblSearch.Location = new Point(1048, 10);
            lblSearch.Name = "lblSearch";
            lblSearch.Padding = new Padding(0, 6, 5, 0);
            lblSearch.Size = new Size(82, 27);
            lblSearch.TabIndex = 1;
            lblSearch.Text = "Tìm kiếm:";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Dock = DockStyle.Left;
            lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(93, 242, 214);
            lblTitle.Location = new Point(20, 10);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(566, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "👤 QUẢN LÝ TÀI KHOẢN & PHÂN QUYỀN HỆ THỐNG";
            lblTitle.UseMnemonic = false;
            // 
            // pnlDetails
            // 
            pnlDetails.BackColor = Color.FromArgb(14, 21, 22);
            pnlDetails.Controls.Add(btnViewOrders);
            pnlDetails.Controls.Add(btnDelete);
            pnlDetails.Controls.Add(btnSave);
            pnlDetails.Controls.Add(btnAddNew);
            pnlDetails.Controls.Add(btnResetPass);
            pnlDetails.Controls.Add(txtNewPassword);
            pnlDetails.Controls.Add(lblPassTitle);
            pnlDetails.Controls.Add(cboRole);
            pnlDetails.Controls.Add(lblRole);
            pnlDetails.Controls.Add(txtEmail);
            pnlDetails.Controls.Add(lblEmail);
            pnlDetails.Controls.Add(txtPhone);
            pnlDetails.Controls.Add(lblPhone);
            pnlDetails.Controls.Add(txtFullName);
            pnlDetails.Controls.Add(lblFullName);
            pnlDetails.Controls.Add(txtUsername);
            pnlDetails.Controls.Add(lblUsername);
            pnlDetails.Controls.Add(lblDetailTitle);
            pnlDetails.Dock = DockStyle.Right;
            pnlDetails.Location = new Point(790, 60);
            pnlDetails.Name = "pnlDetails";
            pnlDetails.Padding = new Padding(20);
            pnlDetails.Size = new Size(360, 640);
            pnlDetails.TabIndex = 1;
            // 
            // btnViewOrders
            // 
            btnViewOrders.BackColor = Color.FromArgb(20, 61, 53);
            btnViewOrders.Cursor = Cursors.Hand;
            btnViewOrders.FlatAppearance.BorderColor = Color.FromArgb(93, 242, 214);
            btnViewOrders.FlatStyle = FlatStyle.Flat;
            btnViewOrders.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnViewOrders.ForeColor = Color.FromArgb(93, 242, 214);
            btnViewOrders.Location = new Point(20, 580);
            btnViewOrders.Name = "btnViewOrders";
            btnViewOrders.Size = new Size(320, 32);
            btnViewOrders.TabIndex = 16;
            btnViewOrders.Text = "📦 XEM LỊCH SỬ ĐƠN HÀNG CỦA USER NÀY";
            btnViewOrders.UseVisualStyleBackColor = false;
            btnViewOrders.Click += btnViewOrders_Click;
            // 
            // btnDelete
            // 
            btnDelete.BackColor = Color.FromArgb(40, 20, 20);
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.FlatAppearance.BorderColor = Color.FromArgb(248, 113, 113);
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnDelete.ForeColor = Color.FromArgb(248, 113, 113);
            btnDelete.Location = new Point(20, 540);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(320, 34);
            btnDelete.TabIndex = 15;
            btnDelete.Text = "🗑️ XÓA TÀI KHOẢN";
            btnDelete.UseVisualStyleBackColor = false;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnSave
            // 
            btnSave.BackColor = Color.FromArgb(0, 121, 107);
            btnSave.Cursor = Cursors.Hand;
            btnSave.FlatAppearance.BorderColor = Color.FromArgb(93, 242, 214);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(185, 492);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(155, 38);
            btnSave.TabIndex = 14;
            btnSave.Text = "💾 CẬP NHẬT";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            // 
            // btnAddNew
            // 
            btnAddNew.BackColor = Color.FromArgb(16, 78, 139);
            btnAddNew.Cursor = Cursors.Hand;
            btnAddNew.FlatAppearance.BorderColor = Color.FromArgb(96, 165, 250);
            btnAddNew.FlatStyle = FlatStyle.Flat;
            btnAddNew.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnAddNew.ForeColor = Color.White;
            btnAddNew.Location = new Point(20, 492);
            btnAddNew.Name = "btnAddNew";
            btnAddNew.Size = new Size(155, 38);
            btnAddNew.TabIndex = 17;
            btnAddNew.Text = "➕ TẠO MỚI";
            btnAddNew.UseVisualStyleBackColor = false;
            btnAddNew.Click += btnAddNew_Click;
            // 
            // btnResetPass
            // 
            btnResetPass.BackColor = Color.FromArgb(234, 179, 8);
            btnResetPass.Cursor = Cursors.Hand;
            btnResetPass.FlatStyle = FlatStyle.Flat;
            btnResetPass.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnResetPass.ForeColor = Color.Black;
            btnResetPass.Location = new Point(240, 428);
            btnResetPass.Name = "btnResetPass";
            btnResetPass.Size = new Size(100, 26);
            btnResetPass.TabIndex = 13;
            btnResetPass.Text = "Cấp lại";
            btnResetPass.UseVisualStyleBackColor = false;
            btnResetPass.Click += btnResetPass_Click;
            // 
            // txtNewPassword
            // 
            txtNewPassword.BackColor = Color.FromArgb(20, 31, 33);
            txtNewPassword.BorderStyle = BorderStyle.FixedSingle;
            txtNewPassword.ForeColor = Color.White;
            txtNewPassword.Location = new Point(20, 430);
            txtNewPassword.Name = "txtNewPassword";
            txtNewPassword.PasswordChar = '•';
            txtNewPassword.Size = new Size(210, 29);
            txtNewPassword.TabIndex = 12;
            // 
            // lblPassTitle
            // 
            lblPassTitle.AutoSize = true;
            lblPassTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblPassTitle.ForeColor = Color.FromArgb(250, 204, 21);
            lblPassTitle.Location = new Point(20, 408);
            lblPassTitle.Name = "lblPassTitle";
            lblPassTitle.Size = new Size(190, 20);
            lblPassTitle.TabIndex = 11;
            lblPassTitle.Text = "CẤP LẠI MẬT KHẨU MỚI:";
            // 
            // cboRole
            // 
            cboRole.BackColor = Color.FromArgb(20, 31, 33);
            cboRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cboRole.FlatStyle = FlatStyle.Flat;
            cboRole.ForeColor = Color.White;
            cboRole.FormattingEnabled = true;
            cboRole.Items.AddRange(new object[] { "1 - Quản trị viên (Admin)", "2 - Khách hàng (Customer)" });
            cboRole.Location = new Point(20, 350);
            cboRole.Name = "cboRole";
            cboRole.Size = new Size(320, 29);
            cboRole.TabIndex = 10;
            // 
            // lblRole
            // 
            lblRole.AutoSize = true;
            lblRole.ForeColor = Color.FromArgb(141, 164, 164);
            lblRole.Location = new Point(20, 330);
            lblRole.Name = "lblRole";
            lblRole.Size = new Size(144, 21);
            lblRole.TabIndex = 9;
            lblRole.Text = "Vai trò phân quyền:";
            // 
            // txtEmail
            // 
            txtEmail.BackColor = Color.FromArgb(20, 31, 33);
            txtEmail.BorderStyle = BorderStyle.FixedSingle;
            txtEmail.ForeColor = Color.White;
            txtEmail.Location = new Point(20, 285);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(320, 29);
            txtEmail.TabIndex = 8;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.ForeColor = Color.FromArgb(141, 164, 164);
            lblEmail.Location = new Point(20, 265);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(51, 21);
            lblEmail.TabIndex = 7;
            lblEmail.Text = "Email:";
            // 
            // txtPhone
            // 
            txtPhone.BackColor = Color.FromArgb(20, 31, 33);
            txtPhone.BorderStyle = BorderStyle.FixedSingle;
            txtPhone.ForeColor = Color.White;
            txtPhone.Location = new Point(20, 220);
            txtPhone.Name = "txtPhone";
            txtPhone.Size = new Size(320, 29);
            txtPhone.TabIndex = 6;
            // 
            // lblPhone
            // 
            lblPhone.AutoSize = true;
            lblPhone.ForeColor = Color.FromArgb(141, 164, 164);
            lblPhone.Location = new Point(20, 200);
            lblPhone.Name = "lblPhone";
            lblPhone.Size = new Size(104, 21);
            lblPhone.TabIndex = 5;
            lblPhone.Text = "Số điện thoại:";
            // 
            // txtFullName
            // 
            txtFullName.BackColor = Color.FromArgb(20, 31, 33);
            txtFullName.BorderStyle = BorderStyle.FixedSingle;
            txtFullName.ForeColor = Color.White;
            txtFullName.Location = new Point(20, 155);
            txtFullName.Name = "txtFullName";
            txtFullName.Size = new Size(320, 29);
            txtFullName.TabIndex = 4;
            // 
            // lblFullName
            // 
            lblFullName.AutoSize = true;
            lblFullName.ForeColor = Color.FromArgb(141, 164, 164);
            lblFullName.Location = new Point(20, 135);
            lblFullName.Name = "lblFullName";
            lblFullName.Size = new Size(79, 21);
            lblFullName.TabIndex = 3;
            lblFullName.Text = "Họ và tên:";
            // 
            // txtUsername
            // 
            txtUsername.BackColor = Color.FromArgb(20, 31, 33);
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtUsername.ForeColor = Color.FromArgb(93, 242, 214);
            txtUsername.Location = new Point(20, 90);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(320, 29);
            txtUsername.TabIndex = 2;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.ForeColor = Color.FromArgb(141, 164, 164);
            lblUsername.Location = new Point(20, 70);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(199, 21);
            lblUsername.TabIndex = 1;
            lblUsername.Text = "Tên đăng nhập (Username):";
            // 
            // lblDetailTitle
            // 
            lblDetailTitle.Dock = DockStyle.Top;
            lblDetailTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblDetailTitle.ForeColor = Color.FromArgb(93, 242, 214);
            lblDetailTitle.Location = new Point(20, 20);
            lblDetailTitle.Name = "lblDetailTitle";
            lblDetailTitle.Size = new Size(320, 35);
            lblDetailTitle.TabIndex = 0;
            lblDetailTitle.Text = "THÔNG TIN TÀI KHOẢN";
            // 
            // dgvUsers
            // 
            dgvUsers.AllowUserToAddRows = false;
            dgvUsers.AllowUserToDeleteRows = false;
            dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvUsers.BackgroundColor = Color.FromArgb(8, 12, 13);
            dgvUsers.BorderStyle = BorderStyle.None;
            dgvUsers.ColumnHeadersHeight = 36;
            dgvUsers.Dock = DockStyle.Fill;
            dgvUsers.GridColor = Color.FromArgb(30, 69, 62);
            dgvUsers.Location = new Point(0, 60);
            dgvUsers.MultiSelect = false;
            dgvUsers.Name = "dgvUsers";
            dgvUsers.ReadOnly = true;
            dgvUsers.RowHeadersVisible = false;
            dgvUsers.RowHeadersWidth = 51;
            dgvUsers.RowTemplate.Height = 32;
            dgvUsers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUsers.Size = new Size(790, 640);
            dgvUsers.TabIndex = 2;
            dgvUsers.CellClick += dgvUsers_CellClick;
            // 
            // UserManagementForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(8, 12, 13);
            ClientSize = new Size(1150, 700);
            Controls.Add(dgvUsers);
            Controls.Add(pnlDetails);
            Controls.Add(pnlHeader);
            Font = new Font("Segoe UI", 9.5F);
            ForeColor = Color.White;
            MinimumSize = new Size(1000, 650);
            Name = "UserManagementForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "DTBStore - Quản Lý Tài Khoản Người Dùng";
            Load += UserManagementForm_Load;
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlDetails.ResumeLayout(false);
            pnlDetails.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvUsers).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Panel pnlDetails;
        private System.Windows.Forms.Label lblDetailTitle;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblFullName;
        private System.Windows.Forms.TextBox txtFullName;
        private System.Windows.Forms.Label lblPhone;
        private System.Windows.Forms.TextBox txtPhone;
        private System.Windows.Forms.Label lblEmail;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Label lblRole;
        private System.Windows.Forms.ComboBox cboRole;
        private System.Windows.Forms.Label lblPassTitle;
        private System.Windows.Forms.TextBox txtNewPassword;
        private System.Windows.Forms.Button btnResetPass;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnAddNew;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnViewOrders;
        private System.Windows.Forms.DataGridView dgvUsers;
    }
}