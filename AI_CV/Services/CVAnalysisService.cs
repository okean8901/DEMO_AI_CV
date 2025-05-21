using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;
using Azure.AI.TextAnalytics;
using Azure;
using AI_CV.Models;

namespace AI_CV.Services
{
    /// <summary>
    /// Dịch vụ đọc và phân tích CV tự động
    /// Sử dụng 3 công cụ của Azure:
    /// - Computer Vision: đọc chữ từ ảnh
    /// - Form Recognizer: đọc cấu trúc CV
    /// - Text Analytics: phân tích nội dung
    /// </summary>
    public class CVAnalysisService : ICVAnalysisService
    {
        // Các biến kết nối đến các dịch vụ của Azure
        private readonly ComputerVisionClient _visionClient;        // Dịch vụ đọc chữ từ ảnh
        private readonly FormRecognizerClient _formClient;         // Dịch vụ đọc cấu trúc văn bản
        private readonly TextAnalyticsClient _textAnalyticsClient; // Dịch vụ phân tích nội dung
        private readonly string _subscriptionKey;                  // Khóa đăng nhập
        private readonly string _endpoint;                         // Địa chỉ kết nối

        /// <summary>
        /// Khởi tạo dịch vụ với thông tin đăng nhập Azure
        /// </summary>
        public CVAnalysisService(string subscriptionKey, string endpoint, string formRecognizerEndpoint, string formRecognizerKey, string textAnalyticsEndpoint, string textAnalyticsKey)
        {
            _subscriptionKey = subscriptionKey ?? throw new ArgumentNullException(nameof(subscriptionKey));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            // Tạo kết nối đến dịch vụ đọc chữ từ ảnh
            _visionClient = new ComputerVisionClient(new ApiKeyServiceClientCredentials(_subscriptionKey))
            {
                Endpoint = _endpoint
            };

            // Tạo kết nối đến dịch vụ đọc cấu trúc văn bản
            _formClient = new FormRecognizerClient(new Uri(formRecognizerEndpoint), new AzureKeyCredential(formRecognizerKey));
            
            // Tạo kết nối đến dịch vụ phân tích nội dung
            _textAnalyticsClient = new TextAnalyticsClient(new Uri(textAnalyticsEndpoint), new AzureKeyCredential(textAnalyticsKey));
        }

        /// <summary>
        /// Hàm chính để đọc và phân tích CV
        /// Các bước xử lý:
        /// 1. Tạo bản sao file để đọc
        /// 2. Đọc cấu trúc CV
        /// 3. Đọc chữ từ ảnh
        /// 4. Gộp kết quả đọc
        /// 5. Phân tích nội dung
        /// 6. Trích xuất thông tin
        /// </summary>
        public async Task<CVAnalysisResult> AnalyzeCVAsync(string fileName, Stream fileStream)
        {
            // Tạo đối tượng kết quả
            var result = new CVAnalysisResult
            {
                FileName = fileName,
                UploadDate = DateTime.Now,
                AnalysisStatus = "Processing"
            };

            try
            {
                // Tạo bản sao file để tránh lỗi khi đọc nhiều lần
                using var formStream = new MemoryStream();
                using var visionStream = new MemoryStream();
                fileStream.Position = 0;
                await fileStream.CopyToAsync(formStream);
                fileStream.Position = 0;
                await fileStream.CopyToAsync(visionStream);
                formStream.Position = 0;
                visionStream.Position = 0;

                // Bước 1: Đọc cấu trúc CV
                var formResult = await AnalyzeWithFormRecognizerAsync(formStream);
                
                // Bước 2: Đọc chữ từ ảnh
                var ocrResult = await AnalyzeWithComputerVisionAsync(visionStream);
                
                // Bước 3: Gộp kết quả đọc
                var combinedText = CombineResults(formResult, ocrResult);
                
                // Bước 4: Phân tích nội dung
                var semanticAnalysis = await AnalyzeWithTextAnalyticsAsync(combinedText);

                // Bước 5: Chuẩn hóa văn bản
                combinedText = NormalizeText(combinedText);

                // Bước 6: Trích xuất thông tin
                result.ExtractedText = combinedText;
                result.ExtractedInformation = ExtractInformation(combinedText, semanticAnalysis);
                result.AnalysisStatus = "Completed";
            }
            catch (Exception ex)
            {
                result.AnalysisStatus = "Failed";
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Đọc cấu trúc CV bằng Form Recognizer
        /// Giúp nhận diện các phần như tiêu đề, đoạn văn, bảng
        /// </summary>
        private async Task<string> AnalyzeWithFormRecognizerAsync(Stream fileStream)
        {
            // Bắt đầu đọc cấu trúc văn bản
            var operation = await _formClient.StartRecognizeContentAsync(fileStream);
            var result = await operation.WaitForCompletionAsync();
            
            // Ghép các dòng văn bản lại với nhau
            var extractedText = new System.Text.StringBuilder();
            foreach (var page in result.Value)
            {
                foreach (var line in page.Lines)
                {
                    extractedText.AppendLine(line.Text);
                }
            }

            return extractedText.ToString();
        }

        /// <summary>
        /// Đọc chữ từ ảnh bằng Computer Vision
        /// Hữu ích cho CV được scan hoặc chụp ảnh
        /// </summary>
        private async Task<string> AnalyzeWithComputerVisionAsync(Stream fileStream)
        {
            // Bắt đầu đọc chữ từ ảnh
            var textOperation = await _visionClient.ReadInStreamAsync(fileStream);
            var operationLocation = textOperation.OperationLocation;
            var operationId = operationLocation.Substring(operationLocation.LastIndexOf('/') + 1);

            // Đợi kết quả đọc chữ
            ReadOperationResult readResult;
            do
            {
                readResult = await _visionClient.GetReadResultAsync(Guid.Parse(operationId));
                await Task.Delay(1000);
            } while (readResult.Status == OperationStatusCodes.Running || readResult.Status == OperationStatusCodes.NotStarted);

            // Ghép các dòng văn bản lại với nhau
            var extractedText = new System.Text.StringBuilder();
            if (readResult.Status == OperationStatusCodes.Succeeded)
            {
                foreach (var readResultItem in readResult.AnalyzeResult.ReadResults)
                {
                    foreach (var line in readResultItem.Lines)
                    {
                        extractedText.AppendLine(line.Text);
                    }
                }
            }

            return extractedText.ToString();
        }

        /// <summary>
        /// Gộp kết quả đọc từ hai công cụ
        /// Loại bỏ các dòng trùng lặp
        /// </summary>
        private string CombineResults(string formResult, string ocrResult)
        {
            // Tạo tập hợp các dòng không trùng lặp
            var lines = new HashSet<string>();
            foreach (var line in formResult.Split('\n').Concat(ocrResult.Split('\n')))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line.Trim());
                }
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Phân tích nội dung văn bản
        /// - Xác định ngôn ngữ
        /// - Tìm từ khóa quan trọng
        /// - Nhận diện tên người, tổ chức
        /// </summary>
        private async Task<Dictionary<string, object>> AnalyzeWithTextAnalyticsAsync(string text)
        {
            var analysis = new Dictionary<string, object>();

            // Xác định ngôn ngữ của văn bản
            var languageResult = await _textAnalyticsClient.DetectLanguageAsync(text);
            analysis["Language"] = languageResult.Value.Iso6391Name;

            // Tìm các từ khóa quan trọng trong văn bản
            var keyPhrasesResult = await _textAnalyticsClient.ExtractKeyPhrasesAsync(text);
            analysis["KeyPhrases"] = keyPhrasesResult.Value.ToList();

            // Nhận diện tên người, tổ chức trong văn bản
            var entitiesResult = await _textAnalyticsClient.RecognizeEntitiesAsync(text);
            analysis["Entities"] = entitiesResult.Value.ToList();

            return analysis;
        }

        /// <summary>
        /// Trích xuất thông tin từ CV
        /// Tìm: email, số điện thoại, tên, kỹ năng, học vấn, kinh nghiệm
        /// </summary>
        private Dictionary<string, string> ExtractInformation(string text, Dictionary<string, object> semanticAnalysis)
        {
            var information = new Dictionary<string, string>();
            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

            // Tìm địa chỉ email
            var emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
            var emailMatches = Regex.Matches(text, emailPattern);
            if (emailMatches.Count > 0)
            {
                information["Email"] = emailMatches[0].Value;
            }

            // Tìm số điện thoại
            var phonePattern = @"(?:\+84|0)[0-9]{9,10}";
            var phoneMatches = Regex.Matches(text, phonePattern);
            if (phoneMatches.Count > 0)
            {
                information["Phone"] = phoneMatches[0].Value;
            }

            // Tìm tên người từ kết quả phân tích
            if (semanticAnalysis.ContainsKey("Entities"))
            {
                var entities = (List<Azure.AI.TextAnalytics.CategorizedEntity>)semanticAnalysis["Entities"];
                var personEntity = entities.FirstOrDefault(e => e.Category == "Person");
                if (!string.IsNullOrEmpty(personEntity.Text))
                {
                    information["FullName"] = personEntity.Text;
                }
            }

            // Tìm các kỹ năng từ từ khóa
            if (semanticAnalysis.ContainsKey("KeyPhrases"))
            {
                var keyPhrases = (List<string>)semanticAnalysis["KeyPhrases"];
                var skills = new List<string>();
                foreach (var phrase in keyPhrases)
                {
                    if (IsSkillPhrase(phrase))
                    {
                        skills.Add(phrase);
                    }
                }
                if (skills.Any())
                {
                    information["Skills"] = string.Join(", ", skills);
                }
            }

            return information;
        }

        /// <summary>
        /// Kiểm tra xem một cụm từ có phải là kỹ năng không
        /// </summary>
        private bool IsSkillPhrase(string phrase)
        {
            // Danh sách các từ khóa thường gặp trong kỹ năng
            var skillKeywords = new[] { "programming", "language", "framework", "database", "tool", "software", "design", "development", "management", "analysis" };
            return skillKeywords.Any(keyword => phrase.ToLower().Contains(keyword));
        }

        /// <summary>
        /// Chuẩn hóa văn bản
        /// - Loại bỏ khoảng trắng thừa
        /// - Chuẩn hóa dấu câu
        /// </summary>
        private string NormalizeText(string text)
        {
            // Loại bỏ khoảng trắng thừa
            text = Regex.Replace(text, @"\s+", " ");
            // Chuẩn hóa dấu câu
            text = Regex.Replace(text, @"\s*([.,;:!?])\s*", "$1 ");
            return text.Trim();
        }

        /// <summary>
        /// Trích xuất nội dung của một phần trong CV
        /// Ví dụ: phần kỹ năng, phần kinh nghiệm
        /// </summary>
        private string ExtractSection(List<string> lines, string[] sectionHeaders)
        {
            var sectionContent = new List<string>();
            var inSection = false;

            foreach (var line in lines)
            {
                // Kiểm tra xem dòng hiện tại có phải là tiêu đề phần không
                if (sectionHeaders.Any(header => line.ToLower().Contains(header.ToLower())))
                {
                    inSection = true;
                    continue;
                }

                // Nếu đang trong phần, thêm dòng vào nội dung
                if (inSection)
                {
                    // Nếu gặp tiêu đề phần khác, dừng lại
                    if (sectionHeaders.Any(header => line.ToLower().Contains(header.ToLower())))
                    {
                        break;
                    }
                    sectionContent.Add(line);
                }
            }

            return string.Join("\n", sectionContent);
        }

        /// <summary>
        /// Xử lý phần kỹ năng trong CV
        /// Tìm và phân loại các kỹ năng
        /// </summary>
        private string ProcessSkillsSection(string content)
        {
            var skills = new List<string>();
            var lines = content.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

            foreach (var line in lines)
            {
                if (IsSkillCategory(line))
                {
                    var extractedSkills = ExtractSkillsFromLine(line);
                    skills.AddRange(extractedSkills);
                }
            }

            return string.Join(", ", skills.Distinct());
        }

        /// <summary>
        /// Kiểm tra xem một dòng có phải là danh mục kỹ năng không
        /// </summary>
        private bool IsSkillCategory(string line)
        {
            var skillCategories = new[] { "technical", "professional", "soft", "hard", "computer", "language", "programming" };
            return skillCategories.Any(category => line.ToLower().Contains(category));
        }

        /// <summary>
        /// Trích xuất các kỹ năng từ một dòng văn bản
        /// </summary>
        private List<string> ExtractSkillsFromLine(string line)
        {
            var skills = new List<string>();
            // Tách các kỹ năng dựa trên dấu phẩy, dấu chấm phẩy
            var parts = line.Split(new[] { ',', ';', ':', '-' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var skill = part.Trim();
                if (!string.IsNullOrEmpty(skill) && IsSkillPhrase(skill))
                {
                    skills.Add(skill);
                }
            }
            return skills;
        }
    }
} 