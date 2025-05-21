using Microsoft.AspNetCore.Mvc;
using AI_CV.Models;
using AI_CV.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace AI_CV.Controllers
{
    /// <summary>
    /// Controller xử lý việc phân tích CV bằng AI
    /// </summary>
    public class CVAnalysisController : Controller
    {
        // Biến chứa dịch vụ phân tích CV
        private readonly ICVAnalysisService _cvAnalysisService;

        /// <summary>
        /// Hàm khởi tạo, nhận vào dịch vụ phân tích CV
        /// </summary>
        public CVAnalysisController(
            ICVAnalysisService cvAnalysisService
        )
        {
            _cvAnalysisService = cvAnalysisService;
        }

        /// <summary>
        /// Hiển thị trang phân tích CV
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Xử lý việc phân tích CV được tải lên
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Analyze(IFormFile file)
        {
            // Kiểm tra xem người dùng đã chọn file chưa
            if (file == null || file.Length == 0)
            {
                return BadRequest("Vui lòng chọn file để tải lên");
            }

            try
            {
                // Gửi file CV để phân tích bằng AI
                var result = await _cvAnalysisService.AnalyzeCVAsync(file.FileName, file.OpenReadStream());
                
                // Lưu kết quả phân tích vào bộ nhớ tạm
                HttpContext.Session.SetString("AnalysisResult", System.Text.Json.JsonSerializer.Serialize(result));
                
                // Chuyển đến trang hiển thị kết quả
                return RedirectToAction("Result");
            }
            catch (Exception ex)
            {
                // Hiển thị trang lỗi nếu có vấn đề xảy ra
                return View("Error", new ErrorViewModel { ErrorMessage = ex.Message });
            }
        }

        /// <summary>
        /// Hiển thị kết quả phân tích CV
        /// </summary>
        public IActionResult Result()
        {
            // Lấy kết quả phân tích từ bộ nhớ tạm
            var resultJson = HttpContext.Session.GetString("AnalysisResult");
            if (string.IsNullOrEmpty(resultJson))
            {
                // Nếu không có kết quả, quay về trang phân tích
                return RedirectToAction("Index");
            }

            // Chuyển đổi kết quả từ dạng JSON sang đối tượng
            var result = System.Text.Json.JsonSerializer.Deserialize<CVAnalysisResult>(resultJson);
            return View(result);
        }
    }
} 