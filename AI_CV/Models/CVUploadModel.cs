using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AI_CV.Models
{
    public class CVUploadModel
    {
        [Required(ErrorMessage = "Vui lòng chọn file CV")]
        [Display(Name = "Chọn file CV")]
        public IFormFile CVFile { get; set; }

        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
    }
} 