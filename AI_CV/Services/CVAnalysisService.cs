using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using AI_CV.Models;

namespace AI_CV.Services
{
    public class CVAnalysisService : ICVAnalysisService
    {
        private readonly ComputerVisionClient _visionClient;
        private readonly string _subscriptionKey;
        private readonly string _endpoint;

        public CVAnalysisService(string subscriptionKey, string endpoint)
        {
            _subscriptionKey = subscriptionKey ?? throw new ArgumentNullException(nameof(subscriptionKey));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            _visionClient = new ComputerVisionClient(new ApiKeyServiceClientCredentials(_subscriptionKey))
            {
                Endpoint = _endpoint
            };
        }

        public async Task<CVAnalysisResult> AnalyzeCVAsync(string fileName, Stream fileStream)
        {
            var result = new CVAnalysisResult
            {
                FileName = fileName,
                UploadDate = DateTime.Now,
                AnalysisStatus = "Processing"
            };

            try
            {
                var textOperation = await _visionClient.ReadInStreamAsync(fileStream);
                var operationLocation = textOperation.OperationLocation;
                var operationId = operationLocation.Substring(operationLocation.LastIndexOf('/') + 1);

                ReadOperationResult readResult;
                do
                {
                    readResult = await _visionClient.GetReadResultAsync(Guid.Parse(operationId));
                    await Task.Delay(1000);
                } while (readResult.Status == OperationStatusCodes.Running || readResult.Status == OperationStatusCodes.NotStarted);

                if (readResult.Status == OperationStatusCodes.Succeeded)
                {
                    var extractedText = "";
                    foreach (var readResultItem in readResult.AnalyzeResult.ReadResults)
                    {
                        foreach (var line in readResultItem.Lines)
                        {
                            extractedText += line.Text + "\n";
                        }
                    }

                    result.ExtractedText = extractedText;
                    result.ExtractedInformation = ExtractInformation(extractedText);
                    result.AnalysisStatus = "Completed";
                }
                else
                {
                    result.AnalysisStatus = "Failed";
                    result.ErrorMessage = "Không thể trích xuất văn bản từ CV";
                }
            }
            catch (Exception ex)
            {
                result.AnalysisStatus = "Failed";
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private Dictionary<string, string> ExtractInformation(string text)
        {
            var information = new Dictionary<string, string>();

            var emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
            var emailMatch = Regex.Match(text, emailPattern);
            if (emailMatch.Success)
            {
                information["Email"] = emailMatch.Value;
            }

            var phonePattern = @"(?:\+84|0)[0-9]{9,10}";
            var phoneMatch = Regex.Match(text, phonePattern);
            if (phoneMatch.Success)
            {
                information["Phone"] = phoneMatch.Value;
            }

            var namePattern = @"(?:Họ và tên|Họ tên|Tên):\s*([^\n]+)";
            var nameMatch = Regex.Match(text, namePattern);
            if (nameMatch.Success)
            {
                information["FullName"] = nameMatch.Groups[1].Value.Trim();
            }

            var skillsPattern = @"(?:Kỹ năng|Skills):\s*([^\n]+)";
            var skillsMatch = Regex.Match(text, skillsPattern);
            if (skillsMatch.Success)
            {
                information["Skills"] = skillsMatch.Groups[1].Value.Trim();
            }

            var educationPattern = @"(?:Học vấn|Education):\s*([^\n]+)";
            var educationMatch = Regex.Match(text, educationPattern);
            if (educationMatch.Success)
            {
                information["Education"] = educationMatch.Groups[1].Value.Trim();
            }

            var experiencePattern = @"(?:Kinh nghiệm|Experience):\s*([^\n]+)";
            var experienceMatch = Regex.Match(text, experiencePattern);
            if (experienceMatch.Success)
            {
                information["Experience"] = experienceMatch.Groups[1].Value.Trim();
            }

            return information;
        }
    }
} 