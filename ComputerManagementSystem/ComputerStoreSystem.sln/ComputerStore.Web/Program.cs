namespace ComputerStore.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://127.0.0.1:5000/api/";
            if (!apiBaseUrl.EndsWith("/"))
            {
                apiBaseUrl += "/";
            }

            builder.Services.AddHttpClient("StoreAPI", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            });

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseSession();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Product}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}