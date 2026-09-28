using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Awai.Models.Entities
{
    public class SiteInfo : BaseEntity
    {
        [StringLength(150)]
        [Display(Name = "اسم الشركة")]
        public required string Name { get; set; }

        [StringLength(200)]
        [Display(Name = "الشعار النصي")]
        public string Slogan { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "هاتف إضافي")]
        public string? Phone2 { get; set; }

        [StringLength(30)]
        [Display(Name = "واتساب (أرقام فقط مع مفتاح الدولة)")]
        public string? WhatsAppNumber { get; set; }

        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "البريد")]
        public string Email { get; set; } = string.Empty;

        [StringLength(400)]
        [Display(Name = "العنوان")]
        public string Address { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "رابط الخريطة")]
        public string? MapUrl { get; set; }

        [StringLength(1000)]
        [Display(Name = "رابط تضمين الخريطة")]
        public string? MapEmbedUrl { get; set; }

        [Display(Name = "الشعار")]
        public string? LogoUrl { get; set; }

        [NotMapped]
        public IFormFile? Logo { get; set; }
    }
}
