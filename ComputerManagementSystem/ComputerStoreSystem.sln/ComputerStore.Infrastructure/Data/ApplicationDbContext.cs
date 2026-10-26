using ComputerStore.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComputerStore.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Chỉ định rõ tên bảng trong CSDL
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Role>().ToTable("Roles");

            // Cấu hình kiểu dữ liệu tiền tệ decimal
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.UnitPrice)
                .HasColumnType("decimal(18,2)");

            // Chèn dữ liệu mẫu ban đầu (Seed Data)
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Customer" }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Laptop Gaming", Description = "Máy tính xách tay cấu hình cao chơi game" },
                new Category { CategoryId = 2, Name = "Laptop Văn Phòng", Description = "Máy tính mỏng nhẹ pin trâu cho học tập và văn phòng" },
                new Category { CategoryId = 3, Name = "Linh Kiện Máy Tính", Description = "CPU, RAM, VGA, Ổ cứng, Nguồn..." }
            );
        }
    }
}