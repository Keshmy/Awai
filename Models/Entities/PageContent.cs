using System.ComponentModel.DataAnnotations;

namespace Awai.Models.Entities
{
    public class PageContent : BaseEntity
    {
        [StringLength(50)]
        public string Key { get; set; } = string.Empty;

        [Display(Name = "العنوان")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "المحتوى")]
        public string Body { get; set; } = string.Empty;
    }
}
