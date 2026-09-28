using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels.Identity
{
    public class CreateUserVM
    {
        [EmailAddress]
        [Display(Name = "البريد")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "العمر")]
        [Range(18, 100)]
        public int Age { get; set; } = 30;

        [Display(Name = "الصلاحية")]
        public string Role { get; set; } = "Employee";
    }
}
