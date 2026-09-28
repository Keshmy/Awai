using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels.Identity
{
    public class RegisterVM
    {
        [Required]
        [Display(Name = "الاسم")]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "الهاتف")]
        public string? Phone { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        [MinLength(6, ErrorMessage = "كلمة المرور 6 أحرف على الأقل")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        [Compare(nameof(Password), ErrorMessage = "كلمة المرور والتأكيد غير متطابقين")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
