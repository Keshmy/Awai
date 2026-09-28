using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Awai.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Range(18, 100)]
        public int Age { get; set; }

        public DateTime? LastAccessTime { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }
        public bool? Approval { get; set; }

        [ValidateNever]
        public UserProfile? UserProfile { get; set; }

        [ValidateNever]
        public Employee? Employee { get; set; }
    }
}
