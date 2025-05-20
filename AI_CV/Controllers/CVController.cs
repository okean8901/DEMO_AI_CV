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
    public class CVController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ICVAnalysisService _cvAnalysisService;
        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".svg" };

        public CVController(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ICVAnalysisService cvAnalysisService)
        {
            _environment = environment;
            _configuration = configuration;
            _cvAnalysisService = cvAnalysisService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadCV(IFormFile CVFile)
        {
            if (CVFile == null || CVFile.Length == 0)
            {
                // Xử lý lỗi: Không có file nào được tải lên
                ViewBag.ErrorMessage = "Vui lòng chọn một file CV để tải lên.";
                return View("Index");
            }

            var fileExtension = Path.GetExtension(CVFile.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                // Xử lý lỗi: Định dạng file không hợp lệ
                ViewBag.ErrorMessage = "Định dạng file không hợp lệ. Vui lòng tải lên file PDF, JPG, JPEG, PNG hoặc SVG.";
                return View("Index");
            }

            try
            {
                // Phân tích CV bằng cách truyền Stream
                var result = await _cvAnalysisService.AnalyzeCVAsync(CVFile.FileName, CVFile.OpenReadStream());
                
                // Lưu kết quả vào session
                HttpContext.Session.SetString("AnalysisResult", JsonSerializer.Serialize(result));

                return RedirectToAction("AnalysisResult");
            }
            catch (Exception ex)
            {
                // Xử lý lỗi trong quá trình phân tích
                ViewBag.ErrorMessage = $"Lỗi trong quá trình phân tích CV: {ex.Message}";
                return View("Index");
            }
        }

        public IActionResult AnalysisResult()
        {
            var resultJson = HttpContext.Session.GetString("AnalysisResult");
            if (string.IsNullOrEmpty(resultJson))
            {
                // Redirect nếu không có kết quả trong session
                return RedirectToAction("Index");
            }

            var analysisResult = JsonSerializer.Deserialize<CVAnalysisResult>(resultJson);
            return View(analysisResult);
        }
    }
} 