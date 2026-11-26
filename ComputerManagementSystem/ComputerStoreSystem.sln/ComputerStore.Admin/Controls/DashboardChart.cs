using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using ComputerStore.Admin.Services;

namespace ComputerStore.Admin.Controls;

// Biểu đồ GDI+ chạy trực tiếp trong WinForms.
public sealed class DashboardChart : Control
{
    private readonly ToolTip _tooltip = new();
    private IReadOnlyList<DashboardValue> _values = Array.Empty<DashboardValue>();
    private int _hoverIndex = -1;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool HorizontalBars { get; init; }

    public DashboardChart()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(14, 21, 22);
        ForeColor = Color.FromArgb(160, 175, 175);
        Font = new Font("Segoe UI", 9);
        AccessibleName = "Biểu đồ thống kê";
    }

    public void SetData(IReadOnlyList<DashboardValue> values)
    {
        _values = values;
        _hoverIndex = -1;
        _tooltip.SetToolTip(this, "");
        AccessibleDescription = string.Join("; ", values.Select(value =>
            value.Label + " " + value.Value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))));
        Invalidate();
    }

    private RectangleF PlotArea => new(76, 20, Math.Max(1, ClientSize.Width - 100),
        Math.Max(1, ClientSize.Height - 62));

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ClientSize.Width < 180 || ClientSize.Height < 100) return;
        if (_values.Count == 0 || _values.All(value => value.Value == 0))
        {
            TextRenderer.DrawText(e.Graphics, HorizontalBars ? "Chưa có đơn hàng trong kỳ" :
                "Chưa có doanh thu từ đơn hoàn thành", Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (HorizontalBars) DrawBars(e.Graphics);
        else DrawRevenue(e.Graphics);
    }

    private void DrawBars(Graphics graphics)
    {
        var max = Math.Max(1, _values.Max(value => value.Value));
        float left = Math.Min(150, ClientSize.Width * .42f);
        float width = Math.Max(1, ClientSize.Width - left - 65);
        float rowHeight = Math.Min(35, (ClientSize.Height - 20f) / _values.Count);
        using var track = new SolidBrush(Color.FromArgb(25, 43, 43));
        using var fill = new SolidBrush(Color.FromArgb(0, 178, 155));
        using var labelBrush = new SolidBrush(ForeColor);
        for (int index = 0; index < _values.Count; index++)
        {
            var item = _values[index];
            float y = 12 + index * rowHeight;
            graphics.DrawString(item.Label, Font, labelBrush, new RectangleF(10, y, left - 16, rowHeight));
            graphics.FillRectangle(track, left, y + 3, width, 14);
            graphics.FillRectangle(fill, left, y + 3, (float)(item.Value / max) * width, 14);
            graphics.DrawString(item.Value.ToString("N0"), Font, labelBrush, left + width + 8, y);
        }
    }

    private void DrawRevenue(Graphics graphics)
    {
        var plot = PlotArea;
        decimal max = Math.Max(1, _values.Max(value => value.Value));
        using var grid = new Pen(Color.FromArgb(34, 57, 55));
        using var line = new Pen(Color.FromArgb(80, 239, 211), 2.5f);
        using var labelBrush = new SolidBrush(ForeColor);
        using var pointBrush = new SolidBrush(Color.FromArgb(80, 239, 211));

        for (int index = 0; index <= 4; index++)
        {
            float y = plot.Bottom - plot.Height * index / 4;
            graphics.DrawLine(grid, plot.Left, y, plot.Right, y);
            string label = CompactMoney(max * index / 4);
            graphics.DrawString(label, Font, labelBrush, new RectangleF(3, y - 8, 70, 20));
        }

        var points = _values.Select((value, index) => new PointF(
            plot.Left + (float)index / Math.Max(1, _values.Count - 1) * plot.Width,
            plot.Bottom - (float)(value.Value / max) * plot.Height)).ToArray();
        if (points.Length > 1) graphics.DrawLines(line, points);
        if (points.Length <= 31)
            foreach (var point in points)
                graphics.FillEllipse(pointBrush, point.X - 3, point.Y - 3, 6, 6);

        int labelCount = Math.Min(5, _values.Count);
        for (int index = 0; index < labelCount; index++)
        {
            int itemIndex = (int)Math.Round(index * (_values.Count - 1d) / Math.Max(1, labelCount - 1));
            float x = points[itemIndex].X;
            graphics.DrawString(_values[itemIndex].Label, Font, labelBrush,
                new RectangleF(x - 22, plot.Bottom + 12, 50, 25));
        }
    }

    private static string CompactMoney(decimal value)
    {
        if (value >= 1_000_000_000) return (value / 1_000_000_000).ToString("0.#") + " tỷ";
        if (value >= 1_000_000) return (value / 1_000_000).ToString("0.#") + " tr";
        if (value >= 1_000) return (value / 1_000).ToString("0.#") + " ngh";
        return value.ToString("0");
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (HorizontalBars || _values.Count == 0) return;
        var plot = PlotArea;
        if (!plot.Contains(e.Location)) return;
        int index = Math.Clamp((int)Math.Round((e.X - plot.Left) / plot.Width * (_values.Count - 1)), 0, _values.Count - 1);
        if (index == _hoverIndex) return;
        _hoverIndex = index;
        var value = _values[index];
        _tooltip.SetToolTip(this, value.Label + " • " +
            value.Value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tooltip.Dispose();
        base.Dispose(disposing);
    }
}
