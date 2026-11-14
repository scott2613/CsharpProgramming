namespace ComputerStore.Admin.Forms
{
    partial class ProductDetailDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // ProductDetailDialog
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(680, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ProductDetailDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Thông Tin Chi Tiết Sản Phẩm";
            Load += ProductDetailDialog_Load_1;
            ResumeLayout(false);
        }

        #endregion
    }
}