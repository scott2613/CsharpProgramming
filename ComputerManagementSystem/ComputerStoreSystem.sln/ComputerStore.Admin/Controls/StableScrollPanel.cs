using System.Drawing;
using System.Windows.Forms;

namespace ComputerStore.Admin.Controls;

// Chỉ cuộn khi người dùng thao tác thanh cuộn/con lăn.
// WinForms mặc định tự cuộn khi một control con nhận focus, dễ che phần đầu biểu mẫu.
internal sealed class StableScrollPanel : Panel
{
    internal StableScrollPanel()
    {
        AutoScroll = true;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        BackColor = AdminTheme.Surface;
        DoubleBuffered = true;
    }

    protected override Point ScrollToControl(Control activeControl) => DisplayRectangle.Location;
}
