using ComputerStore.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ComputerStore.API.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Hiển thị View trang chủ.
        public IActionResult Index()
        {
            return View();
        }

        // Hiển thị nội dung chính sách quyền riêng tư.
        public IActionResult Privacy()
        {
            return View();
        }

        // Yêu cầu trình duyệt không lưu trang lỗi.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Đưa mã truy vết của hoạt động hoặc của yêu cầu vào trang lỗi để đối chiếu log.
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
