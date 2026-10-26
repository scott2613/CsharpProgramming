using ComputerStore.Core.Entities;
using ComputerStore.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComputerStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/orders - Lấy danh sách đơn hàng kèm cơ chế tự sửa lỗi dữ liệu từ Users
        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] string? search, [FromQuery] string? customerPhone)
        {
            try
            {
                var users = await _context.Users.AsNoTracking().ToListAsync();

                var query = _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(customerPhone))
                {
                    string phone = customerPhone.Trim().ToLower();
                    query = query.Where(o => o.CustomerPhone.ToLower().Contains(phone));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string kw = search.Trim().ToLower();
                    query = query.Where(o => o.CustomerName.ToLower().Contains(kw) ||
                                             o.CustomerPhone.ToLower().Contains(kw) ||
                                             (o.DeliveryAddress != null && o.DeliveryAddress.ToLower().Contains(kw)) ||
                                             o.OrderId.ToString().Contains(kw));
                }

                // SẮP XẾP CHUẨN XÁC: Luôn sắp xếp theo OrderId giảm dần (Đơn mới nhất 16, 15, 14... lên đầu bảng)
                var rawOrders = await query
                    .OrderByDescending(o => o.OrderId)
                    .ToListAsync();

                var orders = rawOrders.Select(o =>
                {
                    var matchedUser = users.FirstOrDefault(u =>
                        (!string.IsNullOrEmpty(o.CustomerName) && u.Username.Equals(o.CustomerName, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(o.CustomerPhone) && (
                            (u.Email != null && u.Email.Equals(o.CustomerPhone, StringComparison.OrdinalIgnoreCase)) ||
                            (u.PhoneNumber != null && u.PhoneNumber.Equals(o.CustomerPhone, StringComparison.OrdinalIgnoreCase)) ||
                            u.Username.Equals(o.CustomerPhone, StringComparison.OrdinalIgnoreCase)
                        ))
                    );

                    string displayName = !string.IsNullOrWhiteSpace(matchedUser?.FullName)
                        ? matchedUser.FullName
                        : (!string.IsNullOrWhiteSpace(o.CustomerName) ? o.CustomerName : "Khách hàng");

                    string displayPhone = o.CustomerPhone;
                    if (string.IsNullOrWhiteSpace(displayPhone) || displayPhone.Contains('@'))
                    {
                        displayPhone = matchedUser?.PhoneNumber ?? (matchedUser?.Username ?? "Chưa có SĐT");
                    }

                    string displayAddress = !string.IsNullOrWhiteSpace(o.DeliveryAddress)
                        ? o.DeliveryAddress
                        : "Chưa cập nhật địa chỉ";

                    return new
                    {
                        o.OrderId,
                        CustomerName = displayName,
                        CustomerPhone = displayPhone,
                        DeliveryAddress = displayAddress,
                        FinalAddress = displayAddress,
                        o.OrderDate,
                        o.TotalAmount,
                        o.Status,
                        OrderDetails = o.OrderDetails.Select(d => new
                        {
                            d.OrderDetailId,
                            d.OrderId,
                            d.ProductId,
                            ProductName = d.Product != null ? d.Product.Name : ("Sản phẩm #" + d.ProductId),
                            d.Quantity,
                            d.UnitPrice,
                            TotalPrice = d.Quantity * d.UnitPrice
                        }).ToList()
                    };
                }).ToList();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi truy vấn danh sách đơn hàng: {ex.Message}");
            }
        }

        // GET: api/orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .Where(o => o.OrderId == id)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.CustomerName,
                        o.CustomerPhone,
                        DeliveryAddress = !string.IsNullOrWhiteSpace(o.DeliveryAddress) ? o.DeliveryAddress : "Chưa có địa chỉ",
                        FinalAddress = !string.IsNullOrWhiteSpace(o.DeliveryAddress) ? o.DeliveryAddress : "Chưa có địa chỉ",
                        o.OrderDate,
                        o.TotalAmount,
                        o.Status,
                        OrderDetails = o.OrderDetails.Select(d => new
                        {
                            d.OrderDetailId,
                            d.OrderId,
                            d.ProductId,
                            ProductName = d.Product != null ? d.Product.Name : ("Sản phẩm #" + d.ProductId),
                            d.Quantity,
                            d.UnitPrice,
                            TotalPrice = d.Quantity * d.UnitPrice
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound($"Không tìm thấy đơn hàng #{id}");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi truy vấn đơn hàng #{id}: {ex.Message}");
            }
        }

        // POST: api/orders - Đặt hàng với Transaction an toàn và trừ tồn kho
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto request)
        {
            if (request == null || request.OrderDetails == null || !request.OrderDetails.Any())
            {
                return BadRequest("Thông tin đơn hàng không hợp lệ hoặc giỏ hàng trống.");
            }

            string finalCustomerName = request.CustomerName?.Trim() ?? string.Empty;
            string finalCustomerPhone = request.CustomerPhone?.Trim() ?? string.Empty;
            string finalDeliveryAddress = (request.DeliveryAddress ?? request.CustomerAddress ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                string uName = request.Username.Trim().ToLower();
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == uName ||
                                              (u.PhoneNumber != null && u.PhoneNumber == uName) ||
                                              (u.Email != null && u.Email.ToLower() == uName));

                if (user != null)
                {
                    if (string.IsNullOrWhiteSpace(finalCustomerName))
                        finalCustomerName = user.FullName;

                    if (string.IsNullOrWhiteSpace(finalCustomerPhone))
                        finalCustomerPhone = user.PhoneNumber ?? user.Username;
                }
            }

            if (string.IsNullOrWhiteSpace(finalCustomerName)) finalCustomerName = "Khách hàng";
            if (string.IsNullOrWhiteSpace(finalCustomerPhone)) finalCustomerPhone = "Chưa cung cấp";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal calculatedTotal = 0;
                var detailsToInsert = new List<OrderDetail>();

                foreach (var detail in request.OrderDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Sản phẩm có mã #{detail.ProductId} không tồn tại.");
                    }

                    if (product.StockQuantity < detail.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Sản phẩm '{product.Name}' chỉ còn {product.StockQuantity} máy trong kho.");
                    }

                    product.StockQuantity -= detail.Quantity;
                    decimal unitPrice = product.Price;
                    calculatedTotal += unitPrice * detail.Quantity;

                    detailsToInsert.Add(new OrderDetail
                    {
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        UnitPrice = unitPrice
                    });
                }

                var newOrder = new Order
                {
                    CustomerName = finalCustomerName,
                    CustomerPhone = finalCustomerPhone,
                    DeliveryAddress = finalDeliveryAddress,
                    OrderDate = DateTime.UtcNow.AddHours(7), // Chuẩn hóa múi giờ Việt Nam (UTC+7)
                    TotalAmount = calculatedTotal,
                    Status = "Chờ xử lý",
                    OrderDetails = detailsToInsert
                };

                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = "Đặt hàng thành công!",
                    OrderId = newOrder.OrderId,
                    TotalAmount = newOrder.TotalAmount
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi xử lý giao dịch đơn hàng: {ex.Message}");
            }
        }

        // PUT: api/orders/{id}/status - Cập nhật trạng thái và hoàn kho nếu hủy đơn
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateDto dto)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound("Không tìm thấy đơn hàng cần cập nhật.");
                }

                if (dto.Status == "Đã hủy" && order.Status != "Đã hủy")
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = await _context.Products.FindAsync(detail.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += detail.Quantity;
                        }
                    }
                }

                order.Status = dto.Status;
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"Đã cập nhật trạng thái đơn hàng #{id} thành '{dto.Status}'" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi cập nhật trạng thái đơn hàng: {ex.Message}");
            }
        }

        // PUT: api/orders/{id}/shipping - Sửa thông tin giao hàng (Tên, SĐT, Địa chỉ)
        [HttpPut("{id}/shipping")]
        public async Task<IActionResult> UpdateOrderShipping(int id, [FromBody] OrderShippingUpdateDto dto)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) return NotFound($"Không tìm thấy đơn hàng #{id}");

                if (!string.IsNullOrWhiteSpace(dto.CustomerName))
                    order.CustomerName = dto.CustomerName.Trim();

                if (!string.IsNullOrWhiteSpace(dto.CustomerPhone))
                    order.CustomerPhone = dto.CustomerPhone.Trim();

                if (!string.IsNullOrWhiteSpace(dto.DeliveryAddress))
                    order.DeliveryAddress = dto.DeliveryAddress.Trim();

                await _context.SaveChangesAsync();
                return Ok(new { Message = $"Cập nhật thông tin giao nhận đơn #{id} thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi cập nhật: {ex.Message}");
            }
        }

        // DELETE: api/orders/{id} - Xóa đơn hàng và hoàn trả tồn kho nếu đơn chưa giao xong
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound($"Không tìm thấy đơn hàng #{id}");
                }

                // Nếu đơn chưa hoàn thành và chưa hủy, hoàn lại số lượng sản phẩm vào kho
                if (order.Status != "Đã hoàn thành" && order.Status != "Đã hủy")
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = await _context.Products.FindAsync(detail.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += detail.Quantity;
                        }
                    }
                }

                _context.OrderDetails.RemoveRange(order.OrderDetails);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { Message = $"Đã xóa thành công đơn hàng #{id}" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi xóa đơn hàng: {ex.Message}");
            }
        }
    }

    public class OrderCreateDto
    {
        public string? Username { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? DeliveryAddress { get; set; }
        public string? CustomerAddress { get; set; }
        public List<OrderDetailCreateDto> OrderDetails { get; set; } = new();
    }

    public class OrderDetailCreateDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class OrderShippingUpdateDto
    {
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? DeliveryAddress { get; set; }
    }
}