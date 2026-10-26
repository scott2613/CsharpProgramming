namespace ComputerStore.Admin.Forms
{
    partial class OrderManagementForm
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

        private void InitializeComponent()
        {
            panelHeader = new Panel();
            btnReload = new Button();
            cboStatusFilter = new ComboBox();
            lblFilter = new Label();
            txtSearchOrder = new TextBox();
            lblSearch = new Label();
            lblTitle = new Label();
            splitContainerMain = new SplitContainer();
            dgvOrders = new DataGridView();
            dgvDetails = new DataGridView();
            lblDetailsHeader = new Label();
            panelAction = new Panel();
            btnDeleteOrder = new Button();
            btnEditShipping = new Button();
            btnSaveStatus = new Button();
            cboNewStatus = new ComboBox();
            lblChange = new Label();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvOrders).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvDetails).BeginInit();
            panelAction.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(14, 21, 22);
            panelHeader.Controls.Add(btnReload);
            panelHeader.Controls.Add(cboStatusFilter);
            panelHeader.Controls.Add(lblFilter);
            panelHeader.Controls.Add(txtSearchOrder);
            panelHeader.Controls.Add(lblSearch);
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(1180, 65);
            panelHeader.TabIndex = 0;
            // 
            // btnReload
            // 
            btnReload.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnReload.BackColor = Color.FromArgb(20, 61, 53);
            btnReload.Cursor = Cursors.Hand;
            btnReload.FlatStyle = FlatStyle.Flat;
            btnReload.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnReload.ForeColor = Color.FromArgb(93, 242, 214);
            btnReload.Location = new Point(1045, 18);
            btnReload.Name = "btnReload";
            btnReload.Size = new Size(115, 32);
            btnReload.TabIndex = 5;
            btnReload.Text = "🔄 Tải lại";
            btnReload.UseVisualStyleBackColor = false;
            btnReload.Click += btnReload_Click;
            // 
            // cboStatusFilter
            // 
            cboStatusFilter.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cboStatusFilter.BackColor = Color.FromArgb(20, 31, 33);
            cboStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cboStatusFilter.FlatStyle = FlatStyle.Flat;
            cboStatusFilter.ForeColor = Color.White;
            cboStatusFilter.FormattingEnabled = true;
            cboStatusFilter.Location = new Point(885, 20);
            cboStatusFilter.Name = "cboStatusFilter";
            cboStatusFilter.Size = new Size(145, 28);
            cboStatusFilter.TabIndex = 4;
            cboStatusFilter.SelectedIndexChanged += cboStatusFilter_SelectedIndexChanged;
            // 
            // lblFilter
            // 
            lblFilter.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblFilter.AutoSize = true;
            lblFilter.ForeColor = Color.FromArgb(141, 164, 164);
            lblFilter.Location = new Point(780, 24);
            lblFilter.Name = "lblFilter";
            lblFilter.Size = new Size(103, 20);
            lblFilter.TabIndex = 3;
            lblFilter.Text = "Lọc trạng thái:";
            // 
            // txtSearchOrder
            // 
            txtSearchOrder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtSearchOrder.BackColor = Color.FromArgb(20, 31, 33);
            txtSearchOrder.BorderStyle = BorderStyle.FixedSingle;
            txtSearchOrder.ForeColor = Color.White;
            txtSearchOrder.Location = new Point(540, 20);
            txtSearchOrder.Name = "txtSearchOrder";
            txtSearchOrder.PlaceholderText = "Tên khách, SĐT, Mã đơn...";
            txtSearchOrder.Size = new Size(220, 27);
            txtSearchOrder.TabIndex = 2;
            txtSearchOrder.TextChanged += txtSearchOrder_TextChanged;
            // 
            // lblSearch
            // 
            lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblSearch.AutoSize = true;
            lblSearch.ForeColor = Color.FromArgb(141, 164, 164);
            lblSearch.Location = new Point(465, 24);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new Size(73, 20);
            lblSearch.TabIndex = 1;
            lblSearch.Text = "Tìm kiếm:";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(93, 242, 214);
            lblTitle.Location = new Point(18, 16);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(249, 31);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "QUẢN LÝ ĐƠN HÀNG";
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new Point(0, 65);
            splitContainerMain.Name = "splitContainerMain";
            splitContainerMain.Orientation = Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(dgvOrders);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(dgvDetails);
            splitContainerMain.Panel2.Controls.Add(lblDetailsHeader);
            splitContainerMain.Panel2.Controls.Add(panelAction);
            splitContainerMain.Size = new Size(1180, 635);
            splitContainerMain.SplitterDistance = 345;
            splitContainerMain.TabIndex = 1;
            // 
            // dgvOrders
            // 
            dgvOrders.AllowUserToAddRows = false;
            dgvOrders.AllowUserToDeleteRows = false;
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvOrders.BackgroundColor = Color.FromArgb(9, 14, 15);
            dgvOrders.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvOrders.Dock = DockStyle.Fill;
            dgvOrders.Location = new Point(0, 0);
            dgvOrders.MultiSelect = false;
            dgvOrders.Name = "dgvOrders";
            dgvOrders.ReadOnly = true;
            dgvOrders.RowHeadersWidth = 51;
            dgvOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvOrders.Size = new Size(1180, 345);
            dgvOrders.TabIndex = 0;
            dgvOrders.CellContentClick += dgvOrders_CellContentClick;
            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;
            // 
            // dgvDetails
            // 
            dgvDetails.AllowUserToAddRows = false;
            dgvDetails.AllowUserToDeleteRows = false;
            dgvDetails.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDetails.BackgroundColor = Color.FromArgb(9, 14, 15);
            dgvDetails.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDetails.Dock = DockStyle.Fill;
            dgvDetails.Location = new Point(0, 34);
            dgvDetails.Name = "dgvDetails";
            dgvDetails.ReadOnly = true;
            dgvDetails.RowHeadersWidth = 51;
            dgvDetails.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDetails.Size = new Size(1180, 192);
            dgvDetails.TabIndex = 1;
            // 
            // lblDetailsHeader
            // 
            lblDetailsHeader.BackColor = Color.FromArgb(14, 21, 22);
            lblDetailsHeader.Dock = DockStyle.Top;
            lblDetailsHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblDetailsHeader.ForeColor = Color.FromArgb(110, 231, 183);
            lblDetailsHeader.Location = new Point(0, 0);
            lblDetailsHeader.Name = "lblDetailsHeader";
            lblDetailsHeader.Padding = new Padding(15, 6, 0, 0);
            lblDetailsHeader.Size = new Size(1180, 34);
            lblDetailsHeader.TabIndex = 2;
            lblDetailsHeader.Text = "CHI TIẾT MẶT HÀNG TRONG ĐƠN:";
            // 
            // panelAction
            // 
            panelAction.BackColor = Color.FromArgb(14, 21, 22);
            panelAction.Controls.Add(btnDeleteOrder);
            panelAction.Controls.Add(btnEditShipping);
            panelAction.Controls.Add(btnSaveStatus);
            panelAction.Controls.Add(cboNewStatus);
            panelAction.Controls.Add(lblChange);
            panelAction.Dock = DockStyle.Bottom;
            panelAction.Location = new Point(0, 226);
            panelAction.Name = "panelAction";
            panelAction.Size = new Size(1180, 60);
            panelAction.TabIndex = 0;
            // 
            // btnDeleteOrder
            // 
            btnDeleteOrder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDeleteOrder.BackColor = Color.FromArgb(40, 20, 20);
            btnDeleteOrder.Cursor = Cursors.Hand;
            btnDeleteOrder.FlatAppearance.BorderColor = Color.FromArgb(248, 113, 113);
            btnDeleteOrder.FlatStyle = FlatStyle.Flat;
            btnDeleteOrder.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnDeleteOrder.ForeColor = Color.FromArgb(248, 113, 113);
            btnDeleteOrder.Location = new Point(1025, 14);
            btnDeleteOrder.Name = "btnDeleteOrder";
            btnDeleteOrder.Size = new Size(135, 34);
            btnDeleteOrder.TabIndex = 4;
            btnDeleteOrder.Text = "🗑️ Xóa đơn";
            btnDeleteOrder.UseVisualStyleBackColor = false;
            btnDeleteOrder.Click += btnDeleteOrder_Click;
            // 
            // btnEditShipping
            // 
            btnEditShipping.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnEditShipping.BackColor = Color.FromArgb(20, 41, 55);
            btnEditShipping.Cursor = Cursors.Hand;
            btnEditShipping.FlatAppearance.BorderColor = Color.FromArgb(96, 165, 250);
            btnEditShipping.FlatStyle = FlatStyle.Flat;
            btnEditShipping.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnEditShipping.ForeColor = Color.FromArgb(147, 197, 253);
            btnEditShipping.Location = new Point(855, 14);
            btnEditShipping.Name = "btnEditShipping";
            btnEditShipping.Size = new Size(160, 34);
            btnEditShipping.TabIndex = 3;
            btnEditShipping.Text = "✏️ Sửa địa chỉ/SĐT";
            btnEditShipping.UseVisualStyleBackColor = false;
            btnEditShipping.Click += btnEditShipping_Click;
            // 
            // btnSaveStatus
            // 
            btnSaveStatus.BackColor = Color.FromArgb(0, 121, 107);
            btnSaveStatus.Cursor = Cursors.Hand;
            btnSaveStatus.FlatAppearance.BorderColor = Color.FromArgb(93, 242, 214);
            btnSaveStatus.FlatStyle = FlatStyle.Flat;
            btnSaveStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSaveStatus.ForeColor = Color.White;
            btnSaveStatus.Location = new Point(410, 14);
            btnSaveStatus.Name = "btnSaveStatus";
            btnSaveStatus.Size = new Size(160, 34);
            btnSaveStatus.TabIndex = 2;
            btnSaveStatus.Text = "💾 Cập nhật";
            btnSaveStatus.UseVisualStyleBackColor = false;
            btnSaveStatus.Click += btnSaveStatus_Click;
            // 
            // cboNewStatus
            // 
            cboNewStatus.BackColor = Color.FromArgb(20, 31, 33);
            cboNewStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cboNewStatus.FlatStyle = FlatStyle.Flat;
            cboNewStatus.ForeColor = Color.White;
            cboNewStatus.FormattingEnabled = true;
            cboNewStatus.Location = new Point(185, 17);
            cboNewStatus.Name = "cboNewStatus";
            cboNewStatus.Size = new Size(205, 28);
            cboNewStatus.TabIndex = 1;
            // 
            // lblChange
            // 
            lblChange.AutoSize = true;
            lblChange.ForeColor = Color.White;
            lblChange.Location = new Point(20, 21);
            lblChange.Name = "lblChange";
            lblChange.Size = new Size(139, 20);
            lblChange.TabIndex = 0;
            lblChange.Text = "Cập nhật trạng thái:";
            // 
            // OrderManagementForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(8, 12, 13);
            ClientSize = new Size(1180, 700);
            Controls.Add(splitContainerMain);
            Controls.Add(panelHeader);
            MinimumSize = new Size(1050, 650);
            Name = "OrderManagementForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "DTBStore - Quản Lý Đơn Hàng";
            Load += OrderManagementForm_Load;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvOrders).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvDetails).EndInit();
            panelAction.ResumeLayout(false);
            panelAction.PerformLayout();
            ResumeLayout(false);
        }


        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.ComboBox cboStatusFilter;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearchOrder;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.DataGridView dgvOrders;
        private System.Windows.Forms.Panel panelAction;
        private System.Windows.Forms.Label lblChange;
        private System.Windows.Forms.ComboBox cboNewStatus;
        private System.Windows.Forms.Button btnSaveStatus;
        private System.Windows.Forms.Button btnEditShipping;
        private System.Windows.Forms.Button btnDeleteOrder;
        private System.Windows.Forms.DataGridView dgvDetails;
        private System.Windows.Forms.Label lblDetailsHeader;
    }
}