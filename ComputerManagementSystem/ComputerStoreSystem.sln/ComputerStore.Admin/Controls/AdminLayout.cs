using System.Drawing;
using System.Windows.Forms;

namespace ComputerStore.Admin.Controls;

// Nhóm nhãn/ô nhập và hàng nút tự tính kích thước, không phụ thuộc tọa độ từng control.
internal static class AdminLayout
{
    internal static TableLayoutPanel Column()
    {
        var table = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 0,
            Margin = Padding.Empty,
            BackColor = AdminTheme.Background
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return table;
    }

    internal static void Row(TableLayoutPanel table, Control child)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(child, 0, row);
    }

    internal static Control Field(Label label, Control input, int width = 240)
    {
        // Chiều rộng do ô cha quyết định; không ép MinimumSize của từng trường.
        // Hàng nhãn và ô nhập có chiều cao xác định, tránh vòng AutoSize theo chiều ngang.
        var group = new FieldPanel(label, input)
        {
            Width = width,
            Margin = new Padding(0, 0, 12, 10)
        };
        return group;
    }

    private sealed class FieldPanel : Panel
    {
        private readonly Label _label;
        private readonly Control _input;
        private readonly int _tallInputHeight;
        private bool _arranging;

        internal FieldPanel(Label label, Control input)
        {
            _label = label;
            _input = input;
            _tallInputHeight = input is TextBox { Multiline: true } || input is Panel ? input.Height : 0;
            label.Padding = Padding.Empty;
            input.Padding = Padding.Empty;
            label.AutoSize = false;
            label.AutoEllipsis = true;
            label.Dock = DockStyle.None;
            label.UseMnemonic = false;
            label.ForeColor = Color.Silver;
            input.Dock = DockStyle.None;
            input.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            label.Margin = input.Margin = Padding.Empty;
            Controls.Add(label);
            Controls.Add(input);
            TabStop = false;
            PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_label == null || _input == null || _arranging) return;
            _arranging = true;
            try
            {
                int gap = Math.Max(5, DeviceDpi / 16);
                using var graphics = _label.IsHandleCreated ? _label.CreateGraphics() : Graphics.FromHwnd(System.IntPtr.Zero);
                int labelHeight = TextRenderer.MeasureText(graphics, _label.Text, _label.Font).Height;
                int inputHeight = _tallInputHeight > 0 ? _tallInputHeight : _input.PreferredSize.Height;
                _label.SetBounds(0, 0, ClientSize.Width, labelHeight);
                _input.SetBounds(0, labelHeight + gap, ClientSize.Width, inputHeight);
                int wanted = labelHeight + gap + inputHeight;
                if (Height != wanted) Height = wanted;
            }
            finally { _arranging = false; }
        }

        // Không dùng chiều rộng hiện tại làm kích thước tối thiểu của bảng cha.
        public override Size GetPreferredSize(Size proposedSize) => new(0, Height);
    }

    internal static FlowLayoutPanel Actions(params Button[] buttons)
    {
        // Chiều cao lấy từ PreferredSize thật của nút sau khi áp dụng font.
        // Hàng AutoSize ở biểu mẫu cha dành đúng phần còn lại cho vùng nhập.
        var flow = new FlowLayoutPanel
        {
            Name = "AdminActionFlow", Dock = DockStyle.Top,
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true, Margin = Padding.Empty, Padding = Padding.Empty,
            BackColor = AdminTheme.Surface
        };
        foreach (var button in buttons)
        {
            AdminTheme.StyleButton(button);
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = Size.Empty;
            button.Dock = DockStyle.None;
            button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            button.Margin = new Padding(0, 0, 8, 6);
            flow.Controls.Add(button);
        }
        return flow;
    }

    internal static FlowLayoutPanel Toolbar(params Control[] controls)
    {
        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            WrapContents = true,
            Margin = new Padding(0, 6, 0, 10),
            Padding = Padding.Empty
        };
        foreach (var control in controls)
        {
            if (control is Button button)
            {
                AdminTheme.StyleButton(button);
                button.Dock = DockStyle.None;
                button.AutoSize = true;
                button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                button.Margin = new Padding(0, 0, 10, 10);
            }
            flow.Controls.Add(control);
        }
        return flow;
    }

    internal static void Heading(Label label, string title)
    {
        label.Text = title; label.AutoSize = true; label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0, 0, 0, 16);
        label.ForeColor = AdminTheme.Accent;
        label.Font = new Font("Segoe UI", 16, FontStyle.Bold);
    }
}
