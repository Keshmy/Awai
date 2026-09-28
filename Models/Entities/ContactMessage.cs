using System.ComponentModel.DataAnnotations;

namespace Awai.Models.Entities
{
    public class ContactMessage : BaseEntity
    {
        [Display(Name = "الاسم")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "الهاتف")]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "البريد")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Display(Name = "الرسالة")]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}
