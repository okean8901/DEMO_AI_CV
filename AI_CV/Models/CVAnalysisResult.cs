using System;
using System.Collections.Generic;

namespace AI_CV.Models
{
    public class CVAnalysisResult
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }
        public string ExtractedText { get; set; }
        public Dictionary<string, string> ExtractedInformation { get; set; }
        public List<string> Skills { get; set; }
        public List<string> Education { get; set; }
        public List<string> Experience { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FullName { get; set; }
        public string AnalysisStatus { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime AnalysisDate { get; set; }
    }
} 