using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels
{
    public class PolicySupportVM
    {
        [Required(ErrorMessage = "يرجى إدخال الاسم")]
        [Display(Name = "الاسم")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم الهاتف")]
        [Display(Name = "رقم الهاتف")]
        [StringLength(40)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "البريد غير صحيح")]
        [Display(Name = "البريد الإلكتروني (اختياري)")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Display(Name = "رقم المعاملة / الوثيقة (إن وجد)")]
        [StringLength(120)]
        public string? PolicyNumber { get; set; }

        [Display(Name = "الرقم الوطني / جواز السفر")]
        [StringLength(80)]
        public string? NationalId { get; set; }

        [Display(Name = "رقم اللوحة")]
        [StringLength(40)]
        public string? PlateNumber { get; set; }

        [Display(Name = "ملاحظات")]
        [StringLength(2000)]
        public string? Message { get; set; }
    }
}
