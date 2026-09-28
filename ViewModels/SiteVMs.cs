using Awai.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels
{
    public class CreateEmployeeVM
    {
        [EmailAddress]
        [Display(Name = "البريد")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Range(18, 100)]
        [Display(Name = "العمر")]
        public int Age { get; set; } = 25;

        public Employee Employee { get; set; } = new();
    }

    public class DashboardVM
    {
        public int ProductsCount { get; set; }
        public int PostsCount { get; set; }
        public int PhotosCount { get; set; }
        public int ApplicationsCount { get; set; }
        public int NewApplications { get; set; }
        public int MessagesCount { get; set; }
        public int PendingUsers { get; set; }
        public List<InsuranceApplication> RecentApplications { get; set; } = [];
    }

    public class HomeVM
    {
        public SiteInfo? Site { get; set; }
        public List<Product> Products { get; set; } = [];
        public List<Post> Posts { get; set; } = [];
        public List<SitePhoto> Photos { get; set; } = [];
        public PageContent? About { get; set; }
    }

    public class NewsListVM
    {
        public List<Post> Posts { get; set; } = [];
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

    public class ApplyVM
    {
        [Display(Name = "نوع التأمين")]
        public Guid ProductId { get; set; }

        [Required]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "الرقم الوطني")]
        public string? NationalId { get; set; }

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [Display(Name = "الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "البريد")]
        public string? Email { get; set; }

        [Display(Name = "العنوان")]
        public string? Address { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "صورة الهوية (اختياري)")]
        public IFormFile? IdDocument { get; set; }

        [Required(ErrorMessage = "أرفق صورة المستند ")]
        [Display(Name = "المستند ")]
        public IFormFile? MainDocument { get; set; }

        [Display(Name = "المستند الإضافي")]
        public IFormFile? ExtraDocument { get; set; }
    }

    public class ContactVM
    {
        [Required]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "البريد")]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "الرسالة")]
        public string Message { get; set; } = string.Empty;
    }
}
