using ComputerStore.Admin.Forms;

namespace ComputerStore.Admin;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // vòng lặp giữ app chạy để quay lại màn hình login khi user bấm đăng xuất
        while (true)
        {
            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK) return;
            }

            using var main = new MainForm();
            Application.Run(main);

            // nếu tắt form chính (X) thì thoát hẳn, chỉ lặp lại khi bấm Logout
            if (!main.LogoutRequested) return;
        }
    }
}