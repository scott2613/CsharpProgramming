using System.ComponentModel.DataAnnotations;

namespace ComputerStore.Web.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên của bạn")]
        [Display(Name = "Họ và tên")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        [Display(Name = "Địa chỉ nhận hàng")]
        public string CustomerAddress { get; set; } = string.Empty;

        public List<CartItem> CartItems { get; set; } = new();

        // ĐỔI DÒNG NÀY ĐỂ CÓ THỂ GÁN GIÁ TRỊ TỪ BÊN NGOÀI
        public decimal TotalAmount { get; set; }
    }
}