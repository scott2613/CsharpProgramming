using ComputerStore.Core.Entities;
using ComputerStore.Application.DTOs.Accounts;
namespace ComputerStore.Application.Interfaces;

public interface IAccountTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}

public interface IAccountRepository
{
    Task<IAccountTransaction> BeginWriteAsync(CancellationToken ct);
    Task<User?> FindByIdAsync(int id, CancellationToken ct);
    Task<List<User>> FindByLoginAsync(string login, CancellationToken ct);
    Task<List<User>> ListAsync(string? keyword, int? roleId, CancellationToken ct);
    Task<Role?> FindRoleAsync(int? id, string? name, CancellationToken ct);
    Task<bool> IdentifierExistsAsync(string username, string? email, string? phone, int? exceptId, CancellationToken ct);
    Task<int> CountAdminsAsync(CancellationToken ct);
    Task<bool> HasOrdersAsync(int userId, CancellationToken ct);
    Task<List<CustomerOrderHistoryDto>> OwnedOrdersAsync(int userId, CancellationToken ct);
    void Add(User user);
    void Remove(User user);
    Task SaveAsync(CancellationToken ct);
}

public interface IAccountPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string password, out bool upgrade);
    void VerifyDummy(string password);
    string Stamp(User user);
}
