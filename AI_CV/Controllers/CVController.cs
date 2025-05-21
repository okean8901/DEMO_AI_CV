using Microsoft.AspNetCore.Mvc;
using AI_CV.Models;
using AI_CV.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;
using System;
using System.Linq;

namespace AI_CV.Controllers
{
    /// <summary>
    /// Controller xử lý các thao tác liên quan đến CV như tải lên và phân tích
    /// </summary>
    public class CVController : Controller
    {
        // Biến lưu thông tin về môi trường chạy website
        private readonly IWebHostEnvironment _environment;
        // Biến lưu các cấu hình của website
        private readonly IConfiguration _configuration;
        // Biến chứa dịch vụ phân tích CV
        private readonly ICVAnalysisService _cvAnalysisService;
        // Danh sách các định dạng file được phép tải lên
        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".svg" };

        /// <summary>
        /// Hàm khởi tạo, nhận vào các dịch vụ cần thiết
        /// </summary>
        public CVController(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ICVAnalysisService cvAnalysisService)
        {
            _environment = environment;
            _configuration = configuration;
            _cvAnalysisService = cvAnalysisService;
        }

        /// <summary>
        /// Hiển thị trang tải lên CV
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Xử lý việc tải lên và phân tích CV
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadCV(IFormFile CVFile)
        {
            // Kiểm tra xem người dùng đã chọn file chưa
            if (CVFile == null || CVFile.Length == 0)
            {
                ViewBag.ErrorMessage = "Vui lòng chọn một file CV để tải lên.";
                return View("Index");
            }

            // Kiểm tra định dạng file có hợp lệ không
            var fileExtension = Path.GetExtension(CVFile.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                ViewBag.ErrorMessage = "Định dạng file không hợp lệ. Vui lòng tải lên file PDF, JPG, JPEG, PNG hoặc SVG.";
                return View("Index");
            }

            try
            {
                // Gửi file CV để phân tích
                var result = await _cvAnalysisService.AnalyzeCVAsync(CVFile.FileName, CVFile.OpenReadStream());
                
                // Lưu kết quả phân tích vào bộ nhớ tạm
                HttpContext.Session.SetString("AnalysisResult", JsonSerializer.Serialize(result));

                // Chuyển đến trang hiển thị kết quả
                return RedirectToAction("AnalysisResult");
            }
            catch (Exception ex)
            {
                // Hiển thị thông báo lỗi nếu có vấn đề xảy ra
                ViewBag.ErrorMessage = $"Lỗi trong quá trình phân tích CV: {ex.Message}";
                return View("Index");
            }
        }

        /// <summary>
        /// Hiển thị kết quả phân tích CV
        /// </summary>
        public IActionResult AnalysisResult()
        {
            // Lấy kết quả phân tích từ bộ nhớ tạm
            var resultJson = HttpContext.Session.GetString("AnalysisResult");
            if (string.IsNullOrEmpty(resultJson))
            {
                // Nếu không có kết quả, quay về trang tải lên
                return RedirectToAction("Index");
            }

            // Chuyển đổi kết quả từ dạng JSON sang đối tượng
            var analysisResult = JsonSerializer.Deserialize<CVAnalysisResult>(resultJson);
            return View(analysisResult);
        }
    }
} 