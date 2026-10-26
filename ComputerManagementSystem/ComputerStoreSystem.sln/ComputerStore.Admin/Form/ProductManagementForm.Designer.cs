namespace ComputerStore.Admin.Forms
{
    partial class ProductManagementForm
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
            dgvProducts = new DataGridView();
            lblTitle = new Label();
            lblName = new Label();
            txtName = new TextBox();
            lblPrice = new Label();
            txtPrice = new TextBox();
            lblStock = new Label();
            numStock = new NumericUpDown();
            lblSpecs = new Label();
            txtSpecs = new TextBox();
            lblImageUrl = new Label();
            txtImageUrl = new TextBox();
            picPreview = new PictureBox();
            btnAdd = new Button();
            btnUpdate = new Button();
            btnDelete = new Button();
            btnClear = new Button();
            btnViewDetail = new Button();
            btnReload = new Button();
            btnOpenOrders = new Button();
            lblSearch = new Label();
            txtSearch = new TextBox();
            ((System.ComponentModel.ISupportInitialize)dgvProducts).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStock).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picPreview).BeginInit();
            SuspendLayout();
            // 
            // dgvProducts
            // 
            dgvProducts.AllowUserToAddRows = false;
            dgvProducts.AllowUserToDeleteRows = false;
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.BorderStyle = BorderStyle.None;
            dgvProducts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvProducts.EnableHeadersVisualStyles = false;
            dgvProducts.GridColor = Color.FromArgb(30, 69, 62);
            dgvProducts.Location = new Point(20, 385);
            dgvProducts.MultiSelect = false;
            dgvProducts.Name = "dgvProducts";
            dgvProducts.ReadOnly = true;
            dgvProducts.RowHeadersWidth = 51;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.Size = new Size(940, 280);
            dgvProducts.TabIndex = 20;
            dgvProducts.CellClick += dgvProducts_CellClick;
            dgvProducts.CellDoubleClick += dgvProducts_CellDoubleClick;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(93, 242, 214);
            lblTitle.Location = new Point(20, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(555, 35);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "QUẢN LÝ SẢN PHẨM & TỒN KHO PHẦN CỨNG";
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.ForeColor = Color.FromArgb(160, 175, 175);
            lblName.Location = new Point(20, 70);
            lblName.Name = "lblName";
            lblName.Size = new Size(67, 20);
            lblName.TabIndex = 2;
            lblName.Text = "Tên máy:";
            // 
            // txtName
            // 
            txtName.BackColor = Color.FromArgb(20, 31, 33);
            txtName.BorderStyle = BorderStyle.FixedSingle;
            txtName.ForeColor = Color.White;
            txtName.Location = new Point(110, 67);
            txtName.Name = "txtName";
            txtName.Size = new Size(500, 27);
            txtName.TabIndex = 3;
            // 
            // lblPrice
            // 
            lblPrice.AutoSize = true;
            lblPrice.ForeColor = Color.FromArgb(160, 175, 175);
            lblPrice.Location = new Point(20, 115);
            lblPrice.Name = "lblPrice";
            lblPrice.Size = new Size(88, 20);
            lblPrice.TabIndex = 4;
            lblPrice.Text = "Đơn giá (đ):";
            // 
            // txtPrice
            // 
            txtPrice.BackColor = Color.FromArgb(20, 31, 33);
            txtPrice.BorderStyle = BorderStyle.FixedSingle;
            txtPrice.ForeColor = Color.White;
            txtPrice.Location = new Point(110, 112);
            txtPrice.Name = "txtPrice";
            txtPrice.Size = new Size(220, 27);
            txtPrice.TabIndex = 5;
            // 
            // lblStock
            // 
            lblStock.AutoSize = true;
            lblStock.ForeColor = Color.FromArgb(160, 175, 175);
            lblStock.Location = new Point(360, 115);
            lblStock.Name = "lblStock";
            lblStock.Size = new Size(100, 20);
            lblStock.TabIndex = 6;
            lblStock.Text = "Số lượng kho:";
            // 
            // numStock
            // 
            numStock.BackColor = Color.FromArgb(20, 31, 33);
            numStock.ForeColor = Color.White;
            numStock.Location = new Point(470, 112);
            numStock.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            numStock.Name = "numStock";
            numStock.Size = new Size(140, 27);
            numStock.TabIndex = 7;
            // 
            // lblSpecs
            // 
            lblSpecs.AutoSize = true;
            lblSpecs.ForeColor = Color.FromArgb(160, 175, 175);
            lblSpecs.Location = new Point(20, 160);
            lblSpecs.Name = "lblSpecs";
            lblSpecs.Size = new Size(69, 20);
            lblSpecs.TabIndex = 8;
            lblSpecs.Text = "Cấu hình:";
            // 
            // txtSpecs
            // 
            txtSpecs.BackColor = Color.FromArgb(20, 31, 33);
            txtSpecs.BorderStyle = BorderStyle.FixedSingle;
            txtSpecs.ForeColor = Color.White;
            txtSpecs.Location = new Point(110, 157);
            txtSpecs.Multiline = true;
            txtSpecs.Name = "txtSpecs";
            txtSpecs.PlaceholderText = "CPU: i7 13700H, RAM: 16GB DDR5, VGA: RTX 4060, SSD: 512GB...";
            txtSpecs.ScrollBars = ScrollBars.Vertical;
            txtSpecs.Size = new Size(500, 65);
            txtSpecs.TabIndex = 9;
            txtSpecs.TextChanged += txtSpecs_TextChanged;
            // 
            // lblImageUrl
            // 
            lblImageUrl.AutoSize = true;
            lblImageUrl.ForeColor = Color.FromArgb(160, 175, 175);
            lblImageUrl.Location = new Point(20, 240);
            lblImageUrl.Name = "lblImageUrl";
            lblImageUrl.Size = new Size(68, 20);
            lblImageUrl.TabIndex = 10;
            lblImageUrl.Text = "URL Ảnh:";
            // 
            // txtImageUrl
            // 
            txtImageUrl.BackColor = Color.FromArgb(20, 31, 33);
            txtImageUrl.BorderStyle = BorderStyle.FixedSingle;
            txtImageUrl.ForeColor = Color.White;
            txtImageUrl.Location = new Point(110, 237);
            txtImageUrl.Name = "txtImageUrl";
            txtImageUrl.Size = new Size(500, 27);
            txtImageUrl.TabIndex = 11;
            txtImageUrl.Leave += txtImageUrl_Leave;
            // 
            // picPreview
            // 
            picPreview.BackColor = Color.FromArgb(14, 21, 22);
            picPreview.BorderStyle = BorderStyle.FixedSingle;
            picPreview.Location = new Point(640, 67);
            picPreview.Name = "picPreview";
            picPreview.Size = new Size(320, 197);
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.TabIndex = 12;
            picPreview.TabStop = false;
            picPreview.Click += picPreview_Click;
            // 
            // btnAdd
            // 
            btnAdd.BackColor = Color.FromArgb(0, 121, 107);
            btnAdd.Cursor = Cursors.Hand;
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnAdd.ForeColor = Color.White;
            btnAdd.Location = new Point(110, 285);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(110, 36);
            btnAdd.TabIndex = 13;
            btnAdd.Text = "+ Thêm mới";
            btnAdd.UseVisualStyleBackColor = false;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnUpdate
            // 
            btnUpdate.BackColor = Color.FromArgb(20, 61, 53);
            btnUpdate.Cursor = Cursors.Hand;
            btnUpdate.FlatAppearance.BorderColor = Color.FromArgb(93, 242, 214);
            btnUpdate.FlatStyle = FlatStyle.Flat;
            btnUpdate.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnUpdate.ForeColor = Color.FromArgb(93, 242, 214);
            btnUpdate.Location = new Point(230, 285);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(130, 36);
            btnUpdate.TabIndex = 14;
            btnUpdate.Text = "💾 Lưu kho & giá";
            btnUpdate.UseVisualStyleBackColor = false;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // btnDelete
            // 
            btnDelete.BackColor = Color.FromArgb(127, 29, 29);
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnDelete.ForeColor = Color.White;
            btnDelete.Location = new Point(370, 285);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(100, 36);
            btnDelete.TabIndex = 15;
            btnDelete.Text = "🗑 Xóa";
            btnDelete.UseVisualStyleBackColor = false;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnClear
            // 
            btnClear.BackColor = Color.FromArgb(30, 41, 45);
            btnClear.Cursor = Cursors.Hand;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.ForeColor = Color.White;
            btnClear.Location = new Point(480, 285);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(100, 36);
            btnClear.TabIndex = 16;
            btnClear.Text = "Làm mới";
            btnClear.UseVisualStyleBackColor = false;
            btnClear.Click += btnClear_Click;
            // 
            // btnViewDetail
            // 
            btnViewDetail.BackColor = Color.FromArgb(20, 61, 53);
            btnViewDetail.Cursor = Cursors.Hand;
            btnViewDetail.FlatAppearance.BorderColor = Color.FromArgb(93, 242, 214);
            btnViewDetail.FlatStyle = FlatStyle.Flat;
            btnViewDetail.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnViewDetail.ForeColor = Color.FromArgb(93, 242, 214);
            btnViewDetail.Location = new Point(590, 285);
            btnViewDetail.Name = "btnViewDetail";
            btnViewDetail.Size = new Size(140, 36);
            btnViewDetail.TabIndex = 17;
            btnViewDetail.Text = "🔍 Xem chi tiết";
            btnViewDetail.UseVisualStyleBackColor = false;
            btnViewDetail.Click += btnViewDetail_Click;
            // 
            // btnReload
            // 
            btnReload.BackColor = Color.FromArgb(30, 41, 45);
            btnReload.Cursor = Cursors.Hand;
            btnReload.FlatStyle = FlatStyle.Flat;
            btnReload.ForeColor = Color.White;
            btnReload.Location = new Point(860, 285);
            btnReload.Name = "btnReload";
            btnReload.Size = new Size(100, 36);
            btnReload.TabIndex = 18;
            btnReload.Text = "🔄 Tải lại";
            btnReload.UseVisualStyleBackColor = false;
            btnReload.Click += btnReload_Click;
            // 
            // btnOpenOrders
            // 
            btnOpenOrders.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenOrders.BackColor = Color.FromArgb(20, 31, 33);
            btnOpenOrders.Cursor = Cursors.Hand;
            btnOpenOrders.FlatAppearance.BorderColor = Color.FromArgb(0, 121, 107);
            btnOpenOrders.FlatStyle = FlatStyle.Flat;
            btnOpenOrders.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnOpenOrders.ForeColor = Color.FromArgb(93, 242, 214);
            btnOpenOrders.Location = new Point(780, 15);
            btnOpenOrders.Name = "btnOpenOrders";
            btnOpenOrders.Size = new Size(180, 35);
            btnOpenOrders.TabIndex = 1;
            btnOpenOrders.Text = "🛒 Xem Đơn Hàng";
            btnOpenOrders.UseVisualStyleBackColor = false;
            btnOpenOrders.Click += btnOpenOrders_Click;
            // 
            // lblSearch
            // 
            lblSearch.AutoSize = true;
            lblSearch.ForeColor = Color.FromArgb(160, 175, 175);
            lblSearch.Location = new Point(20, 345);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new Size(73, 20);
            lblSearch.TabIndex = 19;
            lblSearch.Text = "Tìm kiếm:";
            // 
            // txtSearch
            // 
            txtSearch.BackColor = Color.FromArgb(20, 31, 33);
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.ForeColor = Color.White;
            txtSearch.Location = new Point(110, 342);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Nhập tên máy hoặc thông số CPU / RAM / VGA để tìm nhanh...";
            txtSearch.Size = new Size(850, 27);
            txtSearch.TabIndex = 21;
            txtSearch.TextChanged += txtSearch_TextChanged;
            // 
            // ProductManagementForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(8, 12, 13);
            ClientSize = new Size(982, 683);
            Controls.Add(dgvProducts);
            Controls.Add(txtSearch);
            Controls.Add(lblSearch);
            Controls.Add(btnOpenOrders);
            Controls.Add(btnReload);
            Controls.Add(btnViewDetail);
            Controls.Add(btnClear);
            Controls.Add(btnDelete);
            Controls.Add(btnUpdate);
            Controls.Add(btnAdd);
            Controls.Add(picPreview);
            Controls.Add(txtImageUrl);
            Controls.Add(lblImageUrl);
            Controls.Add(txtSpecs);
            Controls.Add(lblSpecs);
            Controls.Add(numStock);
            Controls.Add(lblStock);
            Controls.Add(txtPrice);
            Controls.Add(lblPrice);
            Controls.Add(txtName);
            Controls.Add(lblName);
            Controls.Add(lblTitle);
            ForeColor = Color.White;
            Name = "ProductManagementForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "DTBStore - Quản Lý Sản Phẩm & Kho Máy Tính";
            Load += ProductManagementForm_Load;
            ((System.ComponentModel.ISupportInitialize)dgvProducts).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStock).EndInit();
            ((System.ComponentModel.ISupportInitialize)picPreview).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Label lblName;
        private TextBox txtName;
        private Label lblPrice;
        private TextBox txtPrice;
        private Label lblStock;
        private NumericUpDown numStock;
        private Label lblSpecs;
        private TextBox txtSpecs;
        private Label lblImageUrl;
        private TextBox txtImageUrl;
        private PictureBox picPreview;
        private Button btnAdd;
        private Button btnUpdate;
        private Button btnDelete;
        private Button btnClear;
        private Button btnViewDetail;
        private Button btnReload;
        private Button btnOpenOrders;
        private Label lblSearch;
        private TextBox txtSearch;
        private DataGridView dgvProducts;
    }
}