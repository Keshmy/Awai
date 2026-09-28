using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Awai.Models.Entities
{
    public class Employee : BaseEntity
    {
        [StringLength(100)]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        [Display(Name = "هاتف العمل")]
        public string? WorkPhone { get; set; }

        [StringLength(200)]
        [Display(Name = "العنوان")]
        public string? Address { get; set; }

        [StringLength(100)]
        [Display(Name = "المسمى الوظيفي")]
        public string? JobTitle { get; set; }

        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; } = null!;

        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; }
    }
}
