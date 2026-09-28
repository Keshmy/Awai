using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels.Identity
{
    public class RoleVM
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UsersCount { get; set; }
    }

    public class CreateRoleVM
    {
        [Display(Name = "اسم الصلاحية")]
        public string Name { get; set; } = string.Empty;
    }
}
