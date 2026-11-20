using System.Data;
using ComputerStore.Application.DTOs.Orders;
using ComputerStore.Application.Interfaces;
using ComputerStore.Core.Entities;
using ComputerStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ComputerStore.Infrastructure.Repositories;

// Kho lưu trữ và xử lý giao dịch đơn hàng, giỏ hàng (hiện thực IOrderStore)
public sealed class OrderStore(ApplicationDbContext db) : IOrderStore
{
    // Kiểm tra người dùng có phải là Admin hay không
    public Task<bool> IsAdminAsync(int userId, CancellationToken ct) => db.Users
        .AnyAsync(u => u.UserId == userId && u.Role != null && u.Role.RoleName == "Admin", ct);

    // Kiểm tra người dùng có phải tài khoản hợp lệ trong hệ thống hay không
    public Task<bool> IsAccountAsync(int userId, CancellationToken ct) => db.Users
        .AnyAsync(u => u.UserId == userId && u.Role != null &&
            (u.Role.RoleName == "Admin" || u.Role.RoleName == "Customer"), ct);

    // Lấy thông tin các sản phẩm trong giỏ hàng theo danh sách ID
    public Task<List<Product>> CartProductsAsync(int[] ids, CancellationToken ct) => db.Products
        .AsNoTracking().Where(p => ids.Contains(p.ProductId)).ToListAsync(ct);

    // Phép chiếu DTO tóm tắt đơn hàng kèm chi tiết các dòng sản phẩm
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

    // Lấy danh sách toàn bộ đơn hàng (hỗ trợ tìm kiếm theo từ khóa hoặc số điện thoại)
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

    // Đọc chi tiết 1 đơn hàng theo ID (kiểm tra quyền sở hữu của khách hoặc quyền Admin)
    public Task<OrderSummaryDto?> ReadAsync(int id, int actorId, bool admin, CancellationToken ct) =>
        Project(db.Orders.AsNoTracking().Where(o => o.OrderId == id && (admin || o.UserId == actorId)))
            .FirstOrDefaultAsync(ct);

    // Mở một giao dịch CSDL (Database Transaction) với mức cô lập Serializable
    public async Task<IOrderTransaction> BeginAsync(CancellationToken ct) =>
        new Transaction(await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct), db);

    // Khóa sản phẩm bằng câu lệnh SQL UPDLOCK, HOLDLOCK để đảm bảo trừ tồn kho an toàn tuyệt đối
    // Tránh race condition khi nhiều khách cùng đặt mua chiếc máy tính cuối cùng
    public Task<Product?> LockProductAsync(int id, CancellationToken ct) => db.Products
        .FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE ProductId = {id}")
        .SingleOrDefaultAsync(ct);

    // Khóa đơn hàng kèm danh sách OrderDetails để cập nhật trạng thái hoặc hoàn kho
    public async Task<Order?> LockOrderAsync(int id, CancellationToken ct)
    {
        var order = await db.Orders
            .FromSqlInterpolated($"SELECT * FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK) WHERE OrderId = {id}")
            .SingleOrDefaultAsync(ct);
        if (order != null) await db.Entry(order).Collection(o => o.OrderDetails).LoadAsync(ct);
        return order;
    }

    // Thêm đơn hàng mới
    public void Add(Order order) => db.Orders.Add(order);

    // Xóa đơn hàng và các dòng chi tiết liên quan
    public void Delete(Order order)
    {
        db.OrderDetails.RemoveRange(order.OrderDetails);
        db.Orders.Remove(order);
    }

    // Lưu các thay đổi vào CSDL
    public async Task SaveAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);

    // Khóa chủ giỏ hàng (User) trước khi đọc/sửa giỏ để chống tạo trùng khóa khi nhấn liên tiếp
    public async Task<bool> LockCartOwnerAsync(int userId, CancellationToken ct) =>
        await db.Users.FromSqlInterpolated(
            $"SELECT * FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE UserId = {userId}")
            .SingleOrDefaultAsync(ct) != null;

    // Đọc danh sách các mục trong giỏ hàng (chế độ chỉ đọc AsNoTracking)
    public Task<List<ShoppingCartItem>> ReadCartAsync(int userId, CancellationToken ct) =>
        db.ShoppingCartItems.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.ProductId).ToListAsync(ct);

    // Đọc danh sách giỏ hàng để cập nhật số lượng
    public Task<List<ShoppingCartItem>> ReadCartForUpdateAsync(int userId, CancellationToken ct) =>
        db.ShoppingCartItems.Where(x => x.UserId == userId).OrderBy(x => x.ProductId).ToListAsync(ct);

    // Thêm mục mới vào giỏ hàng
    public void AddCartItem(ShoppingCartItem item) => db.ShoppingCartItems.Add(item);

    // Xóa các mục khỏi giỏ hàng
    public void RemoveCartItems(IEnumerable<ShoppingCartItem> items) => db.ShoppingCartItems.RemoveRange(items);

    // Lớp nội bộ thực thi IOrderTransaction đóng gói giao dịch CSDL
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
            try { await tx.DisposeAsync(); } // Dispose transaction chưa commit sẽ tự động rollback
            finally { if (!committed) db.ChangeTracker.Clear(); }
        }
    }
}
