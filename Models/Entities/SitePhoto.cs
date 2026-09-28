using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Awai.Models.Entities
{
    public class SitePhoto : BaseEntity
    {
        [Display(Name = "العنوان")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "الصورة")]
        public string ImageUrl { get; set; } = string.Empty;

        [Display(Name = "الترتيب")]
        public int SortOrder { get; set; }

        [Display(Name = "ظاهر")]
        public bool IsPublished { get; set; } = true;

        [NotMapped]
        public IFormFile? Image { get; set; }
    }
}
