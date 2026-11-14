using ComputerStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
namespace ComputerStore.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ShoppingCartItem>(c =>
        {
            c.ToTable("ShoppingCartItems", t => t.HasCheckConstraint("CK_ShoppingCartItems_Quantity", "[Quantity] BETWEEN 1 AND 999"));
            c.HasKey(x => new { x.UserId, x.ProductId });
            c.HasIndex(x => x.ProductId);
            c.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            c.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Product>().ToTable("Products"); modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<Order>().ToTable("Orders"); modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");
        modelBuilder.Entity<User>().ToTable("Users"); modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Order>().Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<OrderDetail>().Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<User>(u => {
            u.Property(x => x.Username).HasMaxLength(50).IsRequired(); u.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            u.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired(); u.Property(x => x.Email).HasMaxLength(100);
            u.Property(x => x.PhoneNumber).HasMaxLength(20); u.Property(x => x.Address).HasMaxLength(255).HasDefaultValue("").IsRequired();
            u.HasIndex(x => x.Username).IsUnique();
            u.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            u.HasIndex(x => x.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL");
            u.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Order>().Property(o => o.DeliveryAddress).HasMaxLength(500);
        modelBuilder.Entity<Order>().HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrderDetail>().HasOne(d => d.Product).WithMany(p => p.OrderDetails).HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Role>().HasData(new Role { RoleId = 1, RoleName = "Admin" }, new Role { RoleId = 2, RoleName = "Customer" });
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1, Name = "Laptop", Description = "Máy tính xách tay cho học tập, văn phòng và chơi game." },
            new Category { CategoryId = 2, Name = "PC", Description = "Máy tính để bàn, thùng máy lắp sẵn và máy tính mini." });
    }
}
