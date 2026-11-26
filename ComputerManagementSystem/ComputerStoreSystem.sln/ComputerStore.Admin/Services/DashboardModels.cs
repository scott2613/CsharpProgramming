using System;
using System.Collections.Generic;

namespace ComputerStore.Admin.Services;

// Một lượt tải hoàn chỉnh. Giao diện chỉ thay dữ liệu sau khi cả ba API thành công.
public sealed class DashboardData
{
    public List<OrderDto> Orders { get; init; } = new();
    public List<ProductDto> Products { get; init; } = new();
    public int CustomerCount { get; init; }
    public DateTimeOffset LoadedAt { get; init; }
}

public sealed class DashboardReport
{
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
    public int PendingCount { get; init; }
    public int CompletedCount { get; init; }
    public List<DashboardValue> DailyRevenue { get; init; } = new();
    public List<DashboardValue> OrderStatuses { get; init; } = new();
    public List<DashboardBestSeller> BestSellers { get; init; } = new();
    public List<OrderDto> RecentOrders { get; init; } = new();
}

public sealed record DashboardValue(string Label, decimal Value);
public sealed record DashboardBestSeller(int ProductId, string ProductName, long Quantity, decimal Revenue);
