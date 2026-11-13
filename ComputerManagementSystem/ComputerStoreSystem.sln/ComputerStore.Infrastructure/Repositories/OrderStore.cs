using System.Data;
using ComputerStore.Application.DTOs.Orders;
using ComputerStore.Application.Interfaces;
using ComputerStore.Core.Entities;
using ComputerStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ComputerStore.Infrastructure.Repositories;
public sealed class OrderStore(ApplicationDbContext db) : IOrderStore
{
    public Task<bool> IsAdminAsync(int userId, CancellationToken ct) => db.Users
        .AnyAsync(u => u.UserId == userId && u.Role != null && u.Role.RoleName == "Admin", ct);
    public Task<bool> IsAccountAsync(int userId, CancellationToken ct) => db.Users
        .AnyAsync(u => u.UserId == userId && u.Role != null &&
            (u.Role.RoleName == "Admin" || u.Role.RoleName == "Customer"), ct);
    public Task<List<Product>> CartProductsAsync(int[] ids, CancellationToken ct) => db.Products
        .AsNoTracking().Where(p => ids.Contains(p.ProductId)).ToListAsync(ct);

    private static IQueryable<OrderSummaryDto> Project(IQueryable<Order> query) => query.Select(o => new OrderSummaryDto
    {
        OrderId = o.OrderId,
        UserId = o.UserId,
        CustomerName = o.CustomerName,
        CustomerPhone = o.CustomerPhone,
        DeliveryAddress = o.DeliveryAddress,
        OrderDate = o.OrderDate,
        TotalAmount = o.TotalAmount,
        Status = o.Status,
        OrderDetails = o.OrderDetails.OrderBy(d => d.OrderDetailId).Select(d => new OrderLineDto
        {
            OrderDetailId = d.OrderDetailId,
            OrderId = d.OrderId,
            ProductId = d.ProductId,
            ProductName = d.Product != null ? d.Product.Name : "Sản phẩm đã ngừng bán",
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice
        }).ToList()
    });
    public Task<List<OrderSummaryDto>> ListAsync(string? search, string? phone, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var term = phone.Trim();
            query = query.Where(o => o.CustomerPhone.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o => o.CustomerName.Contains(term) || o.CustomerPhone.Contains(term) ||
                o.DeliveryAddress.Contains(term) || o.OrderId.ToString().Contains(term));
        }
        return Project(query.OrderByDescending(o => o.OrderId)).ToListAsync(ct);
    }
    public Task<OrderSummaryDto?> ReadAsync(int id, int actorId, bool admin, CancellationToken ct) =>
        Project(db.Orders.AsNoTracking().Where(o => o.OrderId == id && (admin || o.UserId == actorId)))
            .FirstOrDefaultAsync(ct);

    public async Task<IOrderTransaction> BeginAsync(CancellationToken ct) =>
        new Transaction(await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct), db);

    // Tham số được EF parameterize. UPDLOCK giữ khóa tới hết transaction,
    // tránh hai yêu cầu cùng đọc một giá trị tồn kho rồi cùng ghi đè.
    public Task<Product?> LockProductAsync(int id, CancellationToken ct) => db.Products
        .FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE ProductId = {id}")
        .SingleOrDefaultAsync(ct);
    public async Task<Order?> LockOrderAsync(int id, CancellationToken ct)
    {
        var order = await db.Orders
            .FromSqlInterpolated($"SELECT * FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK) WHERE OrderId = {id}")
            .SingleOrDefaultAsync(ct);
        if (order != null) await db.Entry(order).Collection(o => o.OrderDetails).LoadAsync(ct);
        return order;
    }
    public void Add(Order order) => db.Orders.Add(order);
    public void Delete(Order order)
    {
        db.OrderDetails.RemoveRange(order.OrderDetails);
        db.Orders.Remove(order);
    }
    public async Task SaveAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);

    // Mọi luồng ghi giỏ và checkout khóa cùng User trước khi đọc/sửa giỏ.
    // Khóa cả khi giỏ trống để chống hai yêu cầu thêm cùng lúc tạo trùng khóa.
    public async Task<bool> LockCartOwnerAsync(int userId, CancellationToken ct) =>
        await db.Users.FromSqlInterpolated(
            $"SELECT * FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE UserId = {userId}")
            .SingleOrDefaultAsync(ct) != null;
    public Task<List<ShoppingCartItem>> ReadCartAsync(int userId, CancellationToken ct) =>
        db.ShoppingCartItems.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.ProductId).ToListAsync(ct);
    public Task<List<ShoppingCartItem>> ReadCartForUpdateAsync(int userId, CancellationToken ct) =>
        db.ShoppingCartItems.Where(x => x.UserId == userId).OrderBy(x => x.ProductId).ToListAsync(ct);
    public void AddCartItem(ShoppingCartItem item) => db.ShoppingCartItems.Add(item);
    public void RemoveCartItems(IEnumerable<ShoppingCartItem> items) => db.ShoppingCartItems.RemoveRange(items);

    private sealed class Transaction(IDbContextTransaction tx, ApplicationDbContext db) : IOrderTransaction
    {
        private bool committed;
        public async Task CommitAsync(CancellationToken ct)
        {
            await tx.CommitAsync(ct);
            committed = true;
        }
        public async ValueTask DisposeAsync()
        {
            try { await tx.DisposeAsync(); } // Dispose transaction chưa commit sẽ rollback.
            finally { if (!committed) db.ChangeTracker.Clear(); }
        }
    }
}
