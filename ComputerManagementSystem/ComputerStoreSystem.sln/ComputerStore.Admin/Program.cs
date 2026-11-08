using ComputerStore.Admin.Forms;
namespace ComputerStore.Admin;
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        while (true)
        {
            using (var login = new LoginForm())
                if (login.ShowDialog() != DialogResult.OK) return;
            using var main = new MainForm();
            Application.Run(main);
            if (!main.LogoutRequested) return;
        }
    }
}
