using System.Data;
using ComputerStore.Application.DTOs.Accounts;
using ComputerStore.Application.Interfaces;
using ComputerStore.Core.Entities;
using ComputerStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ComputerStore.Infrastructure.Repositories;

public sealed class AccountRepository(ApplicationDbContext db) : IAccountRepository
{
    private sealed class Transaction(IDbContextTransaction transaction, ApplicationDbContext db) : IAccountTransaction
    {
        private bool committed;
        public async Task CommitAsync(CancellationToken ct) { await transaction.CommitAsync(ct); committed = true; }
        public async ValueTask DisposeAsync()
        {
            try { await transaction.DisposeAsync(); }
            finally { if (!committed) db.ChangeTracker.Clear(); }
        }
    }
    public async Task<IAccountTransaction> BeginWriteAsync(CancellationToken ct) =>
        new Transaction(await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct), db);
    public Task<User?> FindByIdAsync(int id, CancellationToken ct) => db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id, ct);
    public Task<List<User>> FindByLoginAsync(string login, CancellationToken ct)
    {
        var key = login.ToLowerInvariant();
        return db.Users.Include(u => u.Role).Where(u => u.Username.ToLower() == key ||
            (u.Email != null && u.Email.ToLower() == key) || (u.PhoneNumber != null && u.PhoneNumber == login)).Take(2).ToListAsync(ct);
    }
    public Task<List<User>> ListAsync(string? keyword, int? roleId, CancellationToken ct)
    {
        var q = db.Users.Include(u => u.Role).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim(); q = q.Where(u => u.Username.Contains(k) || u.FullName.Contains(k) ||
                (u.Email != null && u.Email.Contains(k)) || (u.PhoneNumber != null && u.PhoneNumber.Contains(k)));
        }
        if (roleId.HasValue) q = q.Where(u => u.RoleId == roleId.Value);
        return q.OrderByDescending(u => u.UserId).ToListAsync(ct);
    }
    public Task<Role?> FindRoleAsync(int? id, string? name, CancellationToken ct) => id.HasValue
        ? db.Roles.FirstOrDefaultAsync(r => r.RoleId == id.Value, ct)
        : db.Roles.FirstOrDefaultAsync(r => r.RoleName == name, ct);
    public Task<bool> IdentifierExistsAsync(string username, string? email, string? phone, int? exceptId, CancellationToken ct)
    {
        var keys = new[] { username, email, phone }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.ToLowerInvariant()).Distinct().ToArray();
        return db.Users.AnyAsync(u => (!exceptId.HasValue || u.UserId != exceptId.Value) &&
            (keys.Contains(u.Username.ToLower()) || (u.Email != null && keys.Contains(u.Email.ToLower())) ||
             (u.PhoneNumber != null && keys.Contains(u.PhoneNumber.ToLower()))), ct);
    }
    public Task<int> CountAdminsAsync(CancellationToken ct) => db.Users.CountAsync(u => u.Role != null && u.Role.RoleName == StoreRoles.Admin, ct);
    public Task<bool> HasOrdersAsync(int userId, CancellationToken ct) => db.Orders.AnyAsync(o => o.UserId == userId, ct);
    public Task<List<CustomerOrderHistoryDto>> OwnedOrdersAsync(int userId, CancellationToken ct) =>
        db.Orders.AsNoTracking().Where(o => o.UserId == userId).OrderByDescending(o => o.OrderId)
        .Select(o => new CustomerOrderHistoryDto
        {
            OrderId = o.OrderId,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            DeliveryAddress = o.DeliveryAddress,
            OrderDate = o.OrderDate,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            OrderDetails = o.OrderDetails.Select(d => new CustomerOrderDetailDto
            {
                OrderDetailId = d.OrderDetailId,
                ProductId = d.ProductId,
                ProductName = d.Product == null ? "Sản phẩm đã ngừng bán" : d.Product.Name,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice
            }).ToList()
        }).ToListAsync(ct);
    public void Add(User user) => db.Users.Add(user);
    public void Remove(User user) => db.Users.Remove(user);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
