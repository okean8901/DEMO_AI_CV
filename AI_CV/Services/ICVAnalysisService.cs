using System.Threading.Tasks;
using System.IO;
using AI_CV.Models;

namespace AI_CV.Services
{
    public interface ICVAnalysisService
    {
        // Thay đổi tham số từ string filePath sang Stream fileStream
        Task<CVAnalysisResult> AnalyzeCVAsync(string fileName, Stream fileStream);
    }
} 