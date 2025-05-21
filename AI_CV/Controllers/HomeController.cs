using System.Diagnostics;
using AI_CV.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI_CV.Controllers
{
    /// <summary>
    /// Controller xử lý các trang chính của website
    /// </summary>
    public class HomeController : Controller
    {
        // Biến để ghi lại các thông tin hoạt động của website
        private readonly ILogger<HomeController> _logger;

        // Hàm khởi tạo, nhận vào công cụ ghi log
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Hiển thị trang chủ của website
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Hiển thị trang thông tin về quyền riêng tư
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Hiển thị trang thông báo lỗi khi có vấn đề xảy ra
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
