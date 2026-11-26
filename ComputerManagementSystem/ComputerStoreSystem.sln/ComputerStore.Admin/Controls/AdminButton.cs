using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerStore.Admin.Controls;

// Một cách đo/vẽ chữ cho mọi nút, tránh Button mặc định tự xuống dòng khi thiếu chỗ.
// Kế thừa Button nên vẫn giữ Click, DialogResult, AcceptButton, bàn phím và accessibility.
public sealed class AdminButton : Button
{
    private bool _hovered;
    private bool _pressed;
    private static readonly TextFormatFlags MeasureFlags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

    public AdminButton()
    {
        FlatStyle = FlatStyle.Flat;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        using var graphics = IsHandleCreated ? CreateGraphics() : Graphics.FromHwnd(IntPtr.Zero);
        var text = TextRenderer.MeasureText(graphics, Text ?? "", Font, Size.Empty, MeasureFlags);
        int gap = Math.Max(8, (int)Math.Ceiling(DeviceDpi / 96f * 10));
        int height = Math.Max((int)Math.Ceiling(DeviceDpi / 96f * 32), text.Height + gap);
        return new Size(Math.Max(MinimumSize.Width, text.Width + gap * 2 + 4),
            Math.Max(MinimumSize.Height, height));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var bounds = ClientRectangle;
        if (bounds.Width < 2 || bounds.Height < 2) return;
        Color fill = !Enabled ? Color.FromArgb(24, 34, 35) :
            _pressed ? FlatAppearance.MouseDownBackColor :
            _hovered ? FlatAppearance.MouseOverBackColor : BackColor;
        if (fill.IsEmpty) fill = BackColor;
        using var brush = new SolidBrush(fill);
        e.Graphics.FillRectangle(brush, bounds);
        using var border = new Pen(Enabled ? FlatAppearance.BorderColor : Color.FromArgb(48, 65, 63));
        e.Graphics.DrawRectangle(border, 0, 0, bounds.Width - 1, bounds.Height - 1);
        int inset = Math.Max(6, DeviceDpi / 12);
        var textBounds = Rectangle.Inflate(bounds, -inset, -2);
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
        flags |= TextAlign is ContentAlignment.MiddleLeft or ContentAlignment.TopLeft or ContentAlignment.BottomLeft
            ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter;
        if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds,
            Enabled ? ForeColor : Color.FromArgb(153, 173, 169), flags);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -4, -4), ForeColor, fill);
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { _pressed = true; Invalidate(); } base.OnKeyDown(e); }
    protected override void OnKeyUp(KeyEventArgs e) { _pressed = false; Invalidate(); base.OnKeyUp(e); }
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        Parent?.PerformLayout();
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e) { _pressed = false; Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { _pressed = false; Invalidate(); base.OnLostFocus(e); }
}
