using ComputerStore.Application.DTOs.Dashboard;
using ComputerStore.Application.DTOs.Orders;
using ComputerStore.Application.Services;
using ComputerStore.Application.Interfaces;
using ComputerStore.Application.Exceptions;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
}
static OrderSummaryDto Order(int id, DateTime date, string status, int quantity, decimal price) => new()
{
    OrderId = id, OrderDate = date, Status = status, TotalAmount = quantity * price,
    OrderDetails = new() { new OrderLineDto { ProductId = 57, ProductName = "HP lúc bán", Quantity = quantity, UnitPrice = price } }
};
var day = new DateTime(2026, 11, 26);
var data = new DashboardData
{
    Products = new() { new DashboardProductDto { ProductId = 57, Name = "HP hiện tại" } },
    Orders = new()
    {
        Order(1, day.AddDays(-1), "Đã hoàn thành", 1, 50),
        Order(2, day, "Đã hoàn thành", 2, 100),
        Order(3, day.AddHours(23).AddMinutes(59), "Hoàn thành", 1, 300),
        Order(4, day, "Đã hủy", 10, 900),
        Order(5, day, "Chờ xử lý", 1, 20),
        Order(6, day.AddDays(1), "Đã hoàn thành", 1, 80),
        Order(7, day, "Đang giao hàng", 1, 25),
        Order(8, day, "Trạng thái cũ", 1, 25)
    }
};
var report = DashboardCalculator.Calculate(data, day, day);
Check(report.Revenue == 500 && report.CompletedCount == 2, "Doanh thu chỉ tính đơn hoàn thành và hỗ trợ trạng thái cũ");
Check(report.OrderCount == 6, "Bao gồm cả ngày kết thúc, loại hai ngày ngoài khoảng");
Check(report.PendingCount == 1, "Đếm đúng đơn chờ");
Check(report.BestSellers.Single().Revenue == 500 && report.BestSellers.Single().Quantity == 3, "Top bán chạy dùng đơn giá lúc đặt");
Check(report.BestSellers.Single().ProductName == "HP hiện tại", "Tên sản phẩm tra từ danh mục hiện tại");
Check(report.OrderStatuses.Sum(value => value.Value) == 6 && report.OrderStatuses.Any(value => value.Label == "Trạng thái cũ"), "Trạng thái lạ vẫn được đếm");
Check(report.OrderStatuses.Single(value => value.Label == "Đang giao").Value == 1, "Gộp bí danh trạng thái đang giao");
var extended = DashboardCalculator.Calculate(data, day.AddDays(-3), day);
Check(extended.DailyRevenue.Count == 4 && extended.DailyRevenue[0].Value == 0, "Bổ sung ngày doanh thu bằng 0");
var empty = DashboardCalculator.Calculate(new DashboardData(), day, day);
Check(empty.Revenue == 0 && empty.RecentOrders.Count == 0, "Giỏ dữ liệu trống có số liệu bằng 0");
try { DashboardCalculator.Calculate(data, day, day.AddDays(-1)); throw new Exception("Không chặn ngày đảo"); }
catch (ArgumentException) { Console.WriteLine("PASS Ngày đảo bị từ chối"); }
try { DashboardCalculator.Calculate(data, day, day.AddDays(366)); throw new Exception("Không chặn 367 ngày"); }
catch (ArgumentException) { Console.WriteLine("PASS Khoảng quá 366 ngày bị từ chối"); }
Check(DashboardCalculator.Calculate(data, day.AddDays(-365), day).DailyRevenue.Count == 366, "Cho phép đủ 366 ngày");
var many = new DashboardData
{
    Orders = Enumerable.Range(1, 15).Select(id => Order(id, day, "Đã hoàn thành", 1, 10)).ToList()
};
Check(DashboardCalculator.Calculate(many, day, day).RecentOrders.Select(order => order.OrderId)
    .SequenceEqual(Enumerable.Range(6, 10).Reverse()), "Chỉ hiển thị 10 đơn mới nhất với thứ tự ổn định");
data.Products.Clear();
Check(DashboardCalculator.Calculate(data, day, day).BestSellers.Single().ProductName == "HP lúc bán",
    "Sản phẩm đã rời danh mục vẫn giữ tên trong đơn");
Check(data.Orders.Count == 8 && data.Orders[3].Status == "Đã hủy", "Tính thống kê giữ dữ liệu nguồn");

var ranked = new DashboardData
{
    Orders = Enumerable.Range(1, 8).Select(id => new OrderSummaryDto
    {
        OrderId = id, OrderDate = day, Status = "Đã hoàn thành", TotalAmount = 10,
        OrderDetails = new() { new OrderLineDto { ProductId = id, ProductName = "SP " + id, Quantity = id, UnitPrice = 10 } }
    }).ToList()
};
Check(DashboardCalculator.Calculate(ranked, day, day).BestSellers.Select(product => product.ProductId)
    .SequenceEqual(new[] { 8, 7, 6, 5, 4 }), "Top 5 xếp theo số lượng bán giảm dần");

var store=new FakeDashboardStore{Data=data};var svc=new DashboardService(store);
var snap=await svc.GetAsync(1,day,day,CancellationToken.None);
Check(snap.Report.Revenue==DashboardCalculator.Calculate(data,day,day).Revenue,"Service giữ công thức doanh thu");
Check(store.From==day&&store.Until==day.AddDays(1),"Khoảng SQL nửa mở bao gồm cuối ngày");
try{await svc.GetAsync(2,day,day,CancellationToken.None);throw new Exception("Quyền");}catch(AccountOperationException e){Check(e.StatusCode==403,"Chặn khách gọi service");}
try{await svc.GetAsync(1,day.AddDays(1),day,CancellationToken.None);throw new Exception("Ngày");}catch(AccountOperationException e){Check(e.StatusCode==400,"Chặn khoảng ngày đảo");}
try{await svc.GetAsync(1,day,day.AddDays(366),CancellationToken.None);throw new Exception("Dài");}catch(AccountOperationException e){Check(e.StatusCode==400,"Chặn kỳ quá dài");}
Console.WriteLine("Dashboard calculation/service checks completed.");
sealed class FakeDashboardStore:IDashboardStore
{
 public DashboardData Data=new();public DateTime From,Until;
 public Task<bool> IsAdminAsync(int actorId,CancellationToken ct)=>Task.FromResult(actorId==1);
 public Task<DashboardData> ReadAsync(DateTime from,DateTime until,CancellationToken ct){From=from;Until=until;return Task.FromResult(Data);}
}
