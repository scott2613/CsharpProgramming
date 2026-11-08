using System.Collections.Generic;
using System;

namespace ComputerStore.Admin.Services;

public class OrderDto
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderDetailDto> OrderDetails { get; set; } = new();

        private string? _finalAddress;
        public string FinalAddress
        {
            get => !string.IsNullOrEmpty(_finalAddress)
                ? _finalAddress
                : (!string.IsNullOrEmpty(DeliveryAddress) ? DeliveryAddress : CustomerAddress);
            set => _finalAddress = value;
        }
    }

    public class OrderDetailDto
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        private decimal? _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice ?? (Quantity * UnitPrice);
            set => _totalPrice = value;
        }
    }

public class ProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public string? Specifications { get; set; }
        public string? ImageUrl { get; set; }
        public int? CategoryId { get; set; }

        public string? Cpu { get; set; }
        public string? Ram { get; set; }
        public string? Vga { get; set; }

        // Bổ sung thuộc tính hiển thị trạng thái tồn kho chuẩn hóa
        public string StockStatus => StockQuantity > 0 ? $"Còn hàng ({StockQuantity})" : "Hết hàng";
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "Customer";
    }
