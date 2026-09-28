using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Awai.Models.Entities
{
    public class InsuranceApplication : BaseEntity
    {
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [Display(Name = "الاسم الكامل")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "الرقم الوطني")]
        [StringLength(30)]
        public string? NationalId { get; set; }

        [Display(Name = "تاريخ الميلاد")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "الهاتف")]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "البريد")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Display(Name = "العنوان")]
        [StringLength(300)]
        public string? Address { get; set; }

        [Display(Name = "ملاحظات")]
        [StringLength(2000)]
        public string? Notes { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;

        [StringLength(500)]
        public string? ExternalReference { get; set; }

        public ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
    }
}
