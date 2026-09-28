using System.ComponentModel.DataAnnotations;

namespace Awai.Models.Entities
{
    public class Product : BaseEntity
    {
        [Display(Name = "اسم التأمين")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "وصف مختصر")]
        [StringLength(500)]
        public string ShortDescription { get; set; } = string.Empty;

        [Display(Name = "التفاصيل")]
        public string? Details { get; set; }

        [Display(Name = "السعر (د.ل)")]
        [Range(0, 9999999)]
        public decimal Price { get; set; }

        [Display(Name = "ملاحظة السعر")]
        [StringLength(80)]
        public string? PriceNote { get; set; } = "يبدأ من / سنوياً";

        public string PriceDisplay => Price <= 0
            ? "حسب العرض"
            : $"{Price:N0} د.ل";

        [Display(Name = "أيقونة Bootstrap")]
        [StringLength(80)]
        public string IconClass { get; set; } = "bi-shield-check";

        [Display(Name = "الترتيب")]
        public int SortOrder { get; set; }

        [Display(Name = "ظاهر للزوار")]
        public bool IsPublished { get; set; } = true;
    }
}
