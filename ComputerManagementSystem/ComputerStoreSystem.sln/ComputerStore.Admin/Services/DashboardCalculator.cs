using System;
using System.Collections.Generic;
using System.Linq;

namespace ComputerStore.Admin.Services;

public static class DashboardCalculator
{
    public const string Pending = "Chờ xử lý";
    public const string Completed = "Đã hoàn thành";

    // Chấp nhận các tên trạng thái cũ mà backend cũng hỗ trợ.
    public static string NormalizeStatus(string? status) => status?.Trim() switch
    {
        "Hoàn thành" => Completed,
        "Đang giao hàng" => "Đang giao",
        null or "" => "Chưa xác định",
        var value => value
    };

    public static DashboardReport Calculate(DashboardData data, DateTime from, DateTime to)
    {
        from = from.Date;
        to = to.Date;
        if (to < from || (to - from).TotalDays > 365)
            throw new ArgumentException("Chọn khoảng thời gian từ 1 đến 366 ngày.");

        // Dùng ngày đặt như API trả về và như màn hình quản lý đơn hàng hiển thị.
        var orders = data.Orders
            .Where(order => order.OrderDate.Date >= from && order.OrderDate.Date <= to)
            .ToList();
        var completed = orders.Where(order => NormalizeStatus(order.Status) == Completed).ToList();
        var daily = completed.GroupBy(order => order.OrderDate.Date)
            .ToDictionary(group => group.Key, group => group.Sum(order => order.TotalAmount));
        var dates = Enumerable.Range(0, (to - from).Days + 1)
            .Select(offset => from.AddDays(offset));
        var names = data.Products.ToDictionary(product => product.ProductId, product => product.Name);

        var bestSellers = completed.SelectMany(order => order.OrderDetails)
            .GroupBy(detail => detail.ProductId)
            .Select(group => new DashboardBestSeller(
                group.Key,
                names.GetValueOrDefault(group.Key) ?? group.First().ProductName,
                group.Sum(detail => (long)detail.Quantity),
                // Giá tại lúc đặt; giá sản phẩm hiện tại không tham gia tính doanh thu đã bán.
                group.Sum(detail => detail.UnitPrice * detail.Quantity)))
            .OrderByDescending(product => product.Quantity)
            .ThenByDescending(product => product.Revenue)
            .ThenBy(product => product.ProductId)
            .Take(5).ToList();

        string[] statuses = { Pending, "Đã xác nhận", "Đang giao", Completed, "Đã hủy" };
        var counts = orders.GroupBy(order => NormalizeStatus(order.Status))
            .ToDictionary(group => group.Key, group => group.Count());
        var statusValues = statuses
            .Select(status => new DashboardValue(status, counts.GetValueOrDefault(status))).ToList();
        statusValues.AddRange(counts.Where(pair => !statuses.Contains(pair.Key))
            .OrderBy(pair => pair.Key)
            .Select(pair => new DashboardValue(pair.Key, pair.Value)));

        return new DashboardReport
        {
            Revenue = completed.Sum(order => order.TotalAmount),
            OrderCount = orders.Count,
            PendingCount = counts.GetValueOrDefault(Pending),
            CompletedCount = completed.Count,
            DailyRevenue = dates.Select(date =>
                new DashboardValue(date.ToString("dd/MM"), daily.GetValueOrDefault(date))).ToList(),
            OrderStatuses = statusValues,
            BestSellers = bestSellers,
            RecentOrders = orders.OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.OrderId).Take(10).ToList()
        };
    }
}
