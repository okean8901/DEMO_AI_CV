using Microsoft.AspNetCore.Mvc;
using AI_CV.Models;
using AI_CV.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace AI_CV.Controllers
{
    public class CVAnalysisController : Controller
    {
        private readonly ICVAnalysisService _cvAnalysisService;

        public CVAnalysisController(
            ICVAnalysisService cvAnalysisService
        )
        {
            _cvAnalysisService = cvAnalysisService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Analyze(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            try
            {
                // Call AnalyzeCVAsync with file name and stream
                var result = await _cvAnalysisService.AnalyzeCVAsync(file.FileName, file.OpenReadStream());
                
                // Lưu kết quả vào session
                HttpContext.Session.SetString("AnalysisResult", System.Text.Json.JsonSerializer.Serialize(result));
                
                return RedirectToAction("Result");
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { ErrorMessage = ex.Message });
            }
        }

        public IActionResult Result()
        {
            var resultJson = HttpContext.Session.GetString("AnalysisResult");
            if (string.IsNullOrEmpty(resultJson))
            {
                return RedirectToAction("Index");
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<CVAnalysisResult>(resultJson);
            return View(result);
        }
    }
} 