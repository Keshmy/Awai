using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels.Identity
{
    public class EditUserVM
    {
        public string Id { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "البريد")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "العمر")]
        public int Age { get; set; }

        [Display(Name = "الاسم الظاهر")]
        public string? DisplayName { get; set; }

        [Display(Name = "معتمد")]
        public bool IsApproved { get; set; }

        public List<string> Roles { get; set; } = [];

        [Display(Name = "الصلاحيات")]
        public List<string> SelectedRoles { get; set; } = [];

        public List<string> AvailableRoles { get; set; } = [];
    }

    public class UserListItemVM
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public bool IsApproved { get; set; }
        public bool IsProtected { get; set; }
        public List<string> Roles { get; set; } = [];
        public DateTime CreatedDate { get; set; }
    }
}
