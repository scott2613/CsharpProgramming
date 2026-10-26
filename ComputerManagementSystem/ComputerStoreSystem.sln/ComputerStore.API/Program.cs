using ComputerStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ComputerStore.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Cấu hình DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Bổ sung CORS policy để các client (Web / Admin) kết nối thông suốt
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Cấu hình Controllers và xử lý tham chiếu vòng
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Kích hoạt CORS trước khi ánh xạ routing/controllers
            app.UseCors("AllowAll");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}