using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Awai.Models.Entities
{
    public class ApplicationDocument : BaseEntity
    {
        public Guid InsuranceApplicationId { get; set; }

        [ForeignKey(nameof(InsuranceApplicationId))]
        public InsuranceApplication InsuranceApplication { get; set; } = null!;

        [Display(Name = "نوع المستند")]
        [StringLength(80)]
        public string DocumentType { get; set; } = string.Empty;

        [Display(Name = "اسم الملف")]
        [StringLength(260)]
        public string OriginalFileName { get; set; } = string.Empty;

        public string FileUrl { get; set; } = string.Empty;
    }
}
