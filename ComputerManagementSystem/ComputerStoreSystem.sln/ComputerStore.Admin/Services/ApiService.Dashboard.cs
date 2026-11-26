using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerStore.Admin.Services;

public partial class ApiService
{
    public async Task<DashboardData> GetDashboardDataAsync(CancellationToken ct)
    {
        // Dùng lại HTTP client đã có cookie đăng nhập. Tải tuần tự vì LastError dùng chung.
        var orders = await ReadOrderJsonAsync<List<OrderDto>>("orders", ct)
            ?? throw new InvalidOperationException(LastError ?? "Không tải được đơn hàng.");
        var products = await ReadOrderJsonAsync<List<ProductDto>>("products", ct)
            ?? throw new InvalidOperationException(LastError ?? "Không tải được sản phẩm.");
        var users = await ReadOrderJsonAsync<List<UserDto>>("users", ct)
            ?? throw new InvalidOperationException(LastError ?? "Không tải được tài khoản.");

        return new DashboardData
        {
            Orders = orders,
            Products = products,
            CustomerCount = users.Count(user =>
                string.Equals(user.RoleName, "Customer", StringComparison.OrdinalIgnoreCase)),
            LoadedAt = DateTimeOffset.UtcNow
        };
    }
}
