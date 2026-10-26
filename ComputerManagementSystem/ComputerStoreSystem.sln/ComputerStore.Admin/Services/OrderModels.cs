using System;
using System.Collections.Generic;

namespace ComputerStore.Admin.Services
{
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
}