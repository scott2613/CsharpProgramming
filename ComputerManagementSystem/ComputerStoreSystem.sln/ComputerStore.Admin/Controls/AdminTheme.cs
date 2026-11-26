using System.Drawing;
using System.Windows.Forms;

namespace ComputerStore.Admin.Controls;

// Một bảng màu chung cho các trang quản trị. Không can thiệp vào xử lý nghiệp vụ.
internal static class AdminTheme
{
    internal static readonly Color Background = Color.FromArgb(8, 12, 13);
    internal static readonly Color Surface = Color.FromArgb(14, 21, 22);
    internal static readonly Color Input = Color.FromArgb(20, 31, 33);
    internal static readonly Color Accent = Color.FromArgb(80, 239, 211);
    internal static readonly Color Border = Color.FromArgb(30, 69, 62);
    internal static readonly Color Selected = Color.FromArgb(0, 100, 87);
    internal static readonly Color Danger = Color.FromArgb(248, 113, 113);

    private static readonly Font ButtonFont = new("Segoe UI", 9f, FontStyle.Bold);

    internal static void StyleButton(Button button)
    {
        bool destructive = button.Name.Contains("Delete") || button.Text.Contains("Xóa") && !button.Text.Contains("ô nhập");
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        bool primary = button.Name.Contains("Save") || button.Name.Contains("Update") ||
            button.Name.Contains("Add") || button.Text.StartsWith("Lưu") || button.Text.StartsWith("Đăng nhập");
        button.BackColor = primary ? Selected : Input;
        button.ForeColor = destructive ? Danger : Accent;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = Selected;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 121, 107);
        button.Font = ButtonFont;
        button.Padding = new Padding(10, 4, 10, 4);
        bool fixedAction = button.Parent?.Name == "AdminCompactAction" || button.Name == "AdminMenuButton";
        button.MinimumSize = fixedAction ? Size.Empty : new Size(button.MinimumSize.Width, 32);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.AutoSize = !fixedAction;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Cursor = Cursors.Hand;
    }

    private static void FormatColumns(object? sender, DataGridViewBindingCompleteEventArgs e)
    {
        if (sender is not DataGridView grid) return;
        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.HeaderText = column.HeaderText.Replace('_', ' ');
            var font = grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font;
            int headingWidth = TextRenderer.MeasureText(column.HeaderText, font).Width + 28;
            column.MinimumWidth = Math.Max(72, headingWidth);
        }
    }

    internal static void Apply(Control root)
    {
        switch (root)
        {
            case Button button: StyleButton(button); break;
            case TextBox text:
                text.BackColor = Input; text.ForeColor = Color.White;
                text.BorderStyle = BorderStyle.FixedSingle; break;
            case ComboBox combo:
                combo.BackColor = Input; combo.ForeColor = Color.White;
                combo.FlatStyle = FlatStyle.Flat; break;
            case NumericUpDown numeric:
                numeric.BackColor = Input; numeric.ForeColor = Color.White; break;
            case DataGridView grid:
                grid.BackgroundColor = Surface;
                grid.BorderStyle = BorderStyle.None;
                grid.GridColor = Border;
                grid.EnableHeadersVisualStyles = false;
                // Tiêu đề một hàng ổn định; cột dài dùng thanh cuộn ngang và tooltip.
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                grid.ColumnHeadersHeight = 42;
                grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
                grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 5, 4, 5);
                grid.RowTemplate.Height = 34;
                grid.ShowCellToolTips = true;
                grid.DataBindingComplete -= FormatColumns;
                grid.DataBindingComplete += FormatColumns;
                grid.ColumnHeadersDefaultCellStyle.BackColor = Selected;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Accent;
                grid.DefaultCellStyle.BackColor = Surface;
                grid.DefaultCellStyle.ForeColor = Color.White;
                grid.DefaultCellStyle.SelectionBackColor = Selected;
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                grid.AlternatingRowsDefaultCellStyle.BackColor = Input;
                return;
            case Form form:
                form.BackColor = Background; form.ForeColor = Color.White; break;
        }
        foreach (Control child in root.Controls) Apply(child);
    }
}
