using System.Security.Cryptography;
using System.Text;
using ComputerStore.Application.Interfaces;
using ComputerStore.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ComputerStore.API.Security;

public sealed class LegacyPasswordOptions { public bool AllowLegacyPasswords { get; set; } = true; }

public sealed class AccountPasswordService : IAccountPasswordService
{
    private readonly PasswordHasher<User> _hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 210000 }));
    private readonly User _dummy = new();
    private readonly bool _legacy;
    public AccountPasswordService(IOptions<LegacyPasswordOptions> options)
    { _legacy = options.Value.AllowLegacyPasswords; _dummy.PasswordHash = _hasher.HashPassword(_dummy, Guid.NewGuid().ToString()); }
    public string Hash(User user, string password) => _hasher.HashPassword(user, password);
    public void VerifyDummy(string password) => _hasher.VerifyHashedPassword(_dummy, _dummy.PasswordHash, password);
    private static bool Same(string a, string b) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    public bool Verify(User user, string password, out bool upgrade)
    {
        upgrade = false;
        var stored = user.PasswordHash;
        if (string.IsNullOrWhiteSpace(stored))
        {
            VerifyDummy(password);
            return false;
        }
        // Nhận diện hash Identity; không cho dùng chính hash làm mật khẩu.
        byte[]? decoded = null;
        try { decoded = Convert.FromBase64String(stored); } catch (FormatException) { }
        if ((decoded is { Length: 49 } && decoded[0] == 0) || (decoded is { Length: >= 61 } && decoded[0] == 1))
        {
            try
            {
                var result = _hasher.VerifyHashedPassword(user, stored, password);
                upgrade = result == PasswordVerificationResult.SuccessRehashNeeded;
                return result != PasswordVerificationResult.Failed;
            }
            catch (FormatException) { return false; }
        }
        if (!_legacy) return false;
        bool ok;
        if (stored.Length == 64 && stored.All(Uri.IsHexDigit))
            ok = Same(stored.ToUpperInvariant(), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))));
        else ok = Same(stored, password);
        upgrade = ok; return ok;
    }
    public string Stamp(User user) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        $"{user.UserId}|{user.RoleId}|{user.Username}|{user.PasswordHash}")));
}
