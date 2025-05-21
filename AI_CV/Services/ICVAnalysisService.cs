using System.Threading.Tasks;
using System.IO;
using AI_CV.Models;

namespace AI_CV.Services
{
    /// <summary>
    /// Giao diện định nghĩa các chức năng phân tích CV
    /// </summary>
    public interface ICVAnalysisService
    {
        /// <summary>
        /// Phân tích nội dung của file CV
        /// </summary>
        /// <param name="fileName">Tên file CV</param>
        /// <param name="fileStream">Nội dung file CV</param>
        /// <returns>Kết quả phân tích CV bao gồm thông tin cá nhân, kỹ năng, kinh nghiệm...</returns>
        Task<CVAnalysisResult> AnalyzeCVAsync(string fileName, Stream fileStream);
    }
} 