using ComputerStore.Application.DTOs.Accounts;
namespace ComputerStore.Application.Interfaces;
public interface IAccountService
{
    Task<AuthenticatedAccount?> AuthenticateAsync(AccountLoginDto dto, CancellationToken ct);
    Task<AuthenticatedAccount?> SessionAsync(int userId, CancellationToken ct);
    Task<AccountDto> RegisterAsync(AccountRegisterDto dto, CancellationToken ct);
    Task<AccountDto> ProfileAsync(int userId, CancellationToken ct);
    Task<AccountDto> UpdateProfileAsync(int userId, AccountProfileUpdateDto dto, CancellationToken ct);
    Task<AuthenticatedAccount?> PasswordResetTargetAsync(string email, CancellationToken ct);
    Task ResetByEmailAsync(int userId, string email, string stamp, string password, CancellationToken ct);
    Task<int?> FindCustomerIdAsync(string identifier, CancellationToken ct);
    Task ChangeCustomerPasswordAsync(int userId, AccountPasswordChangeDto dto, CancellationToken ct);
    Task ChangePasswordAsync(int userId, AccountPasswordChangeDto dto, CancellationToken ct);
    Task<List<AccountDto>> ListAsync(int actorId, string? keyword, int? roleId, CancellationToken ct);
    Task<AccountDto> GetByAdminAsync(int actorId, int userId, CancellationToken ct);
    Task<AccountDto> CreateByAdminAsync(int actorId, AdminCreateAccountDto dto, CancellationToken ct);
    Task UpdateByAdminAsync(int actorId, int userId, AdminUpdateAccountDto dto, CancellationToken ct);
    Task ResetByAdminAsync(int actorId, int userId, string password, CancellationToken ct);
    Task DeleteByAdminAsync(int actorId, int userId, CancellationToken ct);
    Task<List<CustomerOrderHistoryDto>> OwnedOrdersAsync(int userId, CancellationToken ct);
}
