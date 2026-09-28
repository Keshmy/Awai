using Awai.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels
{
    public class CompanyContentVM
    {
        public SiteInfo Site { get; set; } = null!;

        [Display(Name = "عنوان صفحة من نحن")]
        public string AboutTitle { get; set; } = string.Empty;

        [Display(Name = "محتوى من نحن")]
        public string AboutBody { get; set; } = string.Empty;

        public Guid AboutId { get; set; }

        [Display(Name = "عنوان صفحة من نكون")]
        public string WhoWeAreTitle { get; set; } = string.Empty;

        [Display(Name = "محتوى من نكون")]
        public string WhoWeAreBody { get; set; } = string.Empty;

        public Guid WhoWeAreId { get; set; }

        public string? KeepLogo { get; set; }
    }
}
