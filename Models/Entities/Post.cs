using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Awai.Models.Entities
{
    public class Post : BaseEntity
    {
        [Display(Name = "العنوان")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "المحتوى")]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "صورة")]
        public string? ImageUrl { get; set; }

        [Display(Name = "تاريخ النشر")]
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "منشور")]
        public bool IsPublished { get; set; } = true;

        [NotMapped]
        public IFormFile? Image { get; set; }
    }
}
