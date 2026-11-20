using ComputerStore.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComputerStore.Infrastructure.Data;

// Ngữ cảnh cơ sở dữ liệu Entity Framework Core 9 của hệ thống BMTDStore
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // Bảng danh mục sản phẩm / máy tính
    public DbSet<Product> Products => Set<Product>();

    // Bảng danh mục phân loại máy tính (Laptop, PC...)
    public DbSet<Category> Categories => Set<Category>();

    // Bảng chứng từ đơn đặt hàng
    public DbSet<Order> Orders => Set<Order>();

    // Bảng dòng chi tiết mặt hàng trong đơn hàng
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    // Bảng hồ sơ tài khoản người dùng (Admin, Customer)
    public DbSet<User> Users => Set<User>();

    // Bảng vai trò và quyền hạn người dùng
    public DbSet<Role> Roles => Set<Role>();

    // Bảng giỏ hàng lưu CSDL cho khách hàng thành viên
    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();

    // Cấu hình lược đồ CSDL, ràng buộc toàn vẹn và dữ liệu mẫu qua Fluent API
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình bảng giỏ hàng ShoppingCartItems
        modelBuilder.Entity<ShoppingCartItem>(c =>
        {
            c.ToTable("ShoppingCartItems", t => t.HasCheckConstraint("CK_ShoppingCartItems_Quantity", "[Quantity] BETWEEN 1 AND 999"));
            c.HasKey(x => new { x.UserId, x.ProductId }); // Khóa chính ghép (UserId + ProductId)
            c.HasIndex(x => x.ProductId);
            c.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); // Xóa user -> xóa giỏ
            c.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade); // Xóa product -> xóa mục giỏ
        });

        // Ánh xạ tên bảng trong CSDL SQL Server
        modelBuilder.Entity<Product>().ToTable("Products"); 
        modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<Order>().ToTable("Orders"); 
        modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");
        modelBuilder.Entity<User>().ToTable("Users"); 
        modelBuilder.Entity<Role>().ToTable("Roles");

        // Cấu hình kiểu dữ liệu tiền tệ decimal(18,2)
        modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Order>().Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<OrderDetail>().Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");

        // Cấu hình bảng Users: ràng buộc độ dài, chỉ mục duy nhất và khóa ngoại
        modelBuilder.Entity<User>(u => {
            u.Property(x => x.Username).HasMaxLength(50).IsRequired(); 
            u.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            u.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired(); 
            u.Property(x => x.Email).HasMaxLength(100);
            u.Property(x => x.PhoneNumber).HasMaxLength(20); 
            u.Property(x => x.Address).HasMaxLength(255).HasDefaultValue("").IsRequired();
            u.HasIndex(x => x.Username).IsUnique(); // Username duy nhất
            u.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL"); // Email duy nhất (bỏ qua null)
            u.HasIndex(x => x.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL"); // SĐT duy nhất (bỏ qua null)
            u.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        // Cấu hình bảng Orders: địa chỉ giao hàng và khóa ngoại trỏ tới Users
        modelBuilder.Entity<Order>().Property(o => o.DeliveryAddress).HasMaxLength(500);
        modelBuilder.Entity<Order>().HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);

        // Cấu hình khóa ngoại Category -> Products (Không cho xóa danh mục nếu còn sản phẩm)
        modelBuilder.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);

        // Cấu hình khóa ngoại Product -> OrderDetails (Không cho xóa sản phẩm nếu đã nằm trong đơn hàng)
        modelBuilder.Entity<OrderDetail>().HasOne(d => d.Product).WithMany(p => p.OrderDetails).HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.Restrict);

        // Khởi tạo dữ liệu mẫu ban đầu (Seed Data) cho vai trò
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin" }, 
            new Role { RoleId = 2, RoleName = "Customer" });

        // Khởi tạo dữ liệu mẫu ban đầu cho danh mục
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1, Name = "Laptop", Description = "Máy tính xách tay cho học tập, văn phòng và chơi game." },
            new Category { CategoryId = 2, Name = "PC", Description = "Máy tính để bàn, thùng máy lắp sẵn và máy tính mini." });
    }
}
