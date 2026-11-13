using System.ComponentModel.DataAnnotations;
using ComputerStore.Application.DTOs.Accounts;
using ComputerStore.Application.Exceptions;
using ComputerStore.Application.Interfaces;
using ComputerStore.Core.Entities;

namespace ComputerStore.Application.Services;

public sealed class AccountService(IAccountRepository repository, IAccountPasswordService passwords) : IAccountService
{
    private static string? Optional(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static void Validate(object dto)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true))
            throw new AccountOperationException(string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    private static void ValidatePassword(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 8 or > 128)
            throw new AccountOperationException("Mật khẩu phải từ 8 đến 128 ký tự.");
    }
    private static AccountDto Map(User u) => new()
    {
        UserId = u.UserId,
        Username = u.Username,
        FullName = u.FullName,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        Address = u.Address,
        RoleId = u.RoleId,
        RoleName = u.Role?.RoleName ?? ""
    };
    private async Task<User> Required(int id, CancellationToken ct) =>
        await repository.FindByIdAsync(id, ct) ?? throw new AccountOperationException("Không tìm thấy tài khoản.", 404);
    private async Task RequireAdmin(int id, CancellationToken ct)
    {
        var u = await Required(id, ct);
        if (u.Role?.RoleName != StoreRoles.Admin) throw new AccountOperationException("Bạn không có quyền quản trị.", 403);
    }
    private async Task<Role> Role(int? id, string? name, CancellationToken ct)
    {
        var role = await repository.FindRoleAsync(id, name, ct);
        if (role is null || !StoreRoles.IsAllowed(role.RoleName))
            throw new AccountOperationException("Chỉ được chọn quyền Admin hoặc Customer.");
        return role;
    }
    private async Task CheckIdentifiers(User u, CancellationToken ct)
    {
        if (await repository.IdentifierExistsAsync(u.Username, u.Email, u.PhoneNumber, u.UserId == 0 ? null : u.UserId, ct))
            throw new AccountOperationException("Tên đăng nhập, email hoặc số điện thoại đã được sử dụng.", 409);
    }

    public async Task<AuthenticatedAccount?> AuthenticateAsync(AccountLoginDto dto, CancellationToken ct)
    {
        Validate(dto);
        await using var tx = await repository.BeginWriteAsync(ct);
        var candidates = await repository.FindByLoginAsync(dto.Username.Trim(), ct);
        if (candidates.Count != 1) { passwords.VerifyDummy(dto.Password); return null; }
        var u = candidates[0];
        if (!passwords.Verify(u, dto.Password, out var upgrade) || !StoreRoles.IsAllowed(u.Role?.RoleName)) return null;
        if (dto.AdminClient && u.Role?.RoleName != StoreRoles.Admin)
            throw new AccountOperationException("Ứng dụng quản trị chỉ dành cho Admin.", 403);
        if (upgrade) { u.PasswordHash = passwords.Hash(u, dto.Password); await repository.SaveAsync(ct); }
        var result = new AuthenticatedAccount(Map(u), passwords.Stamp(u));
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task<AuthenticatedAccount?> SessionAsync(int userId, CancellationToken ct)
    {
        var u = await repository.FindByIdAsync(userId, ct);
        return u is null || !StoreRoles.IsAllowed(u.Role?.RoleName) ? null : new(Map(u), passwords.Stamp(u));
    }
    public async Task<AccountDto> ProfileAsync(int userId, CancellationToken ct) => Map(await Required(userId, ct));

    public async Task<AccountDto> RegisterAsync(AccountRegisterDto dto, CancellationToken ct)
    {
        Validate(dto); ValidatePassword(dto.Password);
        await using var tx = await repository.BeginWriteAsync(ct);
        var role = await Role(null, StoreRoles.Customer, ct);
        var username = Optional(dto.Username) ?? Optional(dto.PhoneNumber) ?? Optional(dto.Email);
        if (username is null || username.Length > 50) throw new AccountOperationException("Tên đăng nhập phải từ 1 đến 50 ký tự.");
        var u = new User
        {
            Username = username,
            FullName = dto.FullName.Trim(),
            Email = Optional(dto.Email),
            PhoneNumber = Optional(dto.PhoneNumber),
            Address = dto.Address?.Trim() ?? "",
            RoleId = role.RoleId,
            Role = role
        };
        await CheckIdentifiers(u, ct);
        u.PasswordHash = passwords.Hash(u, dto.Password); repository.Add(u); await repository.SaveAsync(ct);
        await tx.CommitAsync(ct); return Map(u);
    }

    public async Task<AccountDto> UpdateProfileAsync(int userId, AccountProfileUpdateDto dto, CancellationToken ct)
    {
        Validate(dto);
        await using var tx = await repository.BeginWriteAsync(ct);
        var u = await Required(userId, ct);
        // ID, Username và Role tuyệt đối không lấy từ form hồ sơ.
        u.FullName = dto.FullName.Trim(); u.Email = Optional(dto.Email);
        u.PhoneNumber = dto.PhoneNumber.Trim(); u.Address = dto.Address?.Trim() ?? "";
        await CheckIdentifiers(u, ct); await repository.SaveAsync(ct); await tx.CommitAsync(ct); return Map(u);
    }
    public async Task<AuthenticatedAccount?> PasswordResetTargetAsync(string email, CancellationToken ct)
    {
        var users = await repository.FindByLoginAsync(email, ct);
        var user = users.Count == 1 ? users[0] : null;
        if (user is null || user.Role?.RoleName != StoreRoles.Customer ||
            !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)) return null;
        return new AuthenticatedAccount(Map(user), passwords.Stamp(user));
    }

    public async Task<int?> FindCustomerIdAsync(string identifier, CancellationToken ct)
    {
        var users = await repository.FindByLoginAsync(identifier, ct);
        return users.Count == 1 && users[0].Role?.RoleName == StoreRoles.Customer ? users[0].UserId : null;
    }

    public async Task ResetByEmailAsync(int userId, string email, string stamp, string password, CancellationToken ct)
    {
        ValidatePassword(password);
        await using var tx = await repository.BeginWriteAsync(ct);
        var user = await repository.FindByIdAsync(userId, ct);
        if (user is null || user.Role?.RoleName != StoreRoles.Customer ||
            !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(passwords.Stamp(user), stamp, StringComparison.Ordinal))
            throw new AccountOperationException("Phiên đặt lại đã hết hiệu lực. Vui lòng bắt đầu lại.");
        user.PasswordHash = passwords.Hash(user, password);
        await repository.SaveAsync(ct);
        await tx.CommitAsync(ct);
    }

    public Task ChangePasswordAsync(int userId, AccountPasswordChangeDto dto, CancellationToken ct) =>
        ChangePasswordCoreAsync(userId, dto, customerOnly: false, ct);

    public Task ChangeCustomerPasswordAsync(int userId, AccountPasswordChangeDto dto, CancellationToken ct) =>
        ChangePasswordCoreAsync(userId, dto, customerOnly: true, ct);

    private async Task ChangePasswordCoreAsync(int userId, AccountPasswordChangeDto dto, bool customerOnly, CancellationToken ct)
    {
        Validate(dto); ValidatePassword(dto.NewPassword);
        await using var tx = await repository.BeginWriteAsync(ct);
        var u = await Required(userId, ct);
        if (customerOnly && u.Role?.RoleName != StoreRoles.Customer)
            throw new AccountOperationException("Tài khoản khách hàng không còn hợp lệ.");
        if (!passwords.Verify(u, dto.OldPassword, out _)) throw new AccountOperationException("Mật khẩu hiện tại không chính xác.");
        if (dto.NewPassword == dto.OldPassword) throw new AccountOperationException("Mật khẩu mới phải khác mật khẩu hiện tại.");
        u.PasswordHash = passwords.Hash(u, dto.NewPassword); await repository.SaveAsync(ct); await tx.CommitAsync(ct);
    }

    public async Task<List<AccountDto>> ListAsync(int actorId, string? keyword, int? roleId, CancellationToken ct)
    { await RequireAdmin(actorId, ct); return (await repository.ListAsync(keyword, roleId, ct)).Select(Map).ToList(); }
    public async Task<AccountDto> GetByAdminAsync(int actorId, int userId, CancellationToken ct)
    { await RequireAdmin(actorId, ct); return Map(await Required(userId, ct)); }
    public async Task<AccountDto> CreateByAdminAsync(int actorId, AdminCreateAccountDto dto, CancellationToken ct)
    {
        Validate(dto); ValidatePassword(dto.Password);
        await using var tx = await repository.BeginWriteAsync(ct); await RequireAdmin(actorId, ct);
        var role = await Role(dto.RoleId, null, ct);
        var username = Optional(dto.Username);
        if (username is null || username.Length > 50) throw new AccountOperationException("Tên đăng nhập không hợp lệ.");
        var u = new User
        {
            Username = username,
            FullName = dto.FullName.Trim(),
            Email = Optional(dto.Email),
            PhoneNumber = Optional(dto.PhoneNumber),
            Address = dto.Address?.Trim() ?? "",
            RoleId = role.RoleId,
            Role = role
        };
        await CheckIdentifiers(u, ct); u.PasswordHash = passwords.Hash(u, dto.Password);
        repository.Add(u); await repository.SaveAsync(ct); await tx.CommitAsync(ct); return Map(u);
    }
    public async Task UpdateByAdminAsync(int actorId, int userId, AdminUpdateAccountDto dto, CancellationToken ct)
    {
        Validate(dto);
        await using var tx = await repository.BeginWriteAsync(ct); await RequireAdmin(actorId, ct);
        var u = await Required(userId, ct);
        if (dto.RoleId.HasValue)
        {
            var role = await Role(dto.RoleId, null, ct);
            if (u.Role?.RoleName == StoreRoles.Admin && role.RoleName != StoreRoles.Admin)
            {
                if (actorId == userId) throw new AccountOperationException("Không được tự hạ quyền Admin đang đăng nhập.");
                if (await repository.CountAdminsAsync(ct) <= 1) throw new AccountOperationException("Phải giữ ít nhất một Admin.");
            }
            u.Role = role; u.RoleId = role.RoleId;
        }
        if (dto.Username is not null) { if (string.IsNullOrWhiteSpace(dto.Username)) throw new AccountOperationException("Tên đăng nhập không được trống."); u.Username = dto.Username.Trim(); }
        if (dto.FullName is not null) { if (string.IsNullOrWhiteSpace(dto.FullName)) throw new AccountOperationException("Họ tên không được trống."); u.FullName = dto.FullName.Trim(); }
        u.Email = Optional(dto.Email); u.PhoneNumber = Optional(dto.PhoneNumber);
        if (dto.Address is not null) u.Address = dto.Address?.Trim() ?? "";
        await CheckIdentifiers(u, ct); await repository.SaveAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task ResetByAdminAsync(int actorId, int userId, string password, CancellationToken ct)
    {
        ValidatePassword(password);
        await using var tx = await repository.BeginWriteAsync(ct); await RequireAdmin(actorId, ct);
        var u = await Required(userId, ct); u.PasswordHash = passwords.Hash(u, password);
        await repository.SaveAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task DeleteByAdminAsync(int actorId, int userId, CancellationToken ct)
    {
        await using var tx = await repository.BeginWriteAsync(ct); await RequireAdmin(actorId, ct);
        if (actorId == userId) throw new AccountOperationException("Không được tự xóa tài khoản đang đăng nhập.");
        var u = await Required(userId, ct);
        if (u.Role?.RoleName == StoreRoles.Admin && await repository.CountAdminsAsync(ct) <= 1)
            throw new AccountOperationException("Không thể xóa Admin cuối cùng.");
        if (await repository.HasOrdersAsync(userId, ct)) throw new AccountOperationException("Tài khoản đã có đơn hàng, không thể xóa.", 409);
        repository.Remove(u); await repository.SaveAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<List<CustomerOrderHistoryDto>> OwnedOrdersAsync(int userId, CancellationToken ct)
    { await Required(userId, ct); return await repository.OwnedOrdersAsync(userId, ct); }
}
