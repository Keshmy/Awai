using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels
{
    public class OrangeCardPolicyVM
    {
        [Required(ErrorMessage = "يرجى إدخال اسم المؤمن")]
        [Display(Name = "اسم المؤمن")]
        public string InsuredName { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال العنوان")]
        [Display(Name = "العنوان")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم الهاتف")]
        [Display(Name = "الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال اللوحة المعدنية")]
        [MinLength(2, ErrorMessage = "اللوحة المعدنية قصيرة جداً")]
        [Display(Name = "اللوحة المعدنية")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم الهيكل")]
        [MinLength(2, ErrorMessage = "رقم الهيكل قصير جداً")]
        [Display(Name = "رقم الهيكل")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "رقم الهيكل حروف وأرقام فقط")]
        public string ChassisNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم المحرك")]
        [MinLength(1, ErrorMessage = "يرجى إدخال رقم المحرك")]
        [Display(Name = "رقم المحرك")]
        public string MotorNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال سنة الصنع")]
        [Range(1900, 2050, ErrorMessage = "سنة بين 1900 و 2050")]
        [Display(Name = "سنة الصنع")]
        public int? ManufactureYear { get; set; }

        [Required(ErrorMessage = "اختر نوع المركبة")]
        [Display(Name = "النوع")]
        public int? CarsId { get; set; }

        [Required(ErrorMessage = "اختر جنسية المركبة")]
        [Display(Name = "جنسية المركبة")]
        public int? VehicleNationalityId { get; set; } = 1;

        [Required]
        [Display(Name = "تاريخ البدء (بتوقيت ليبيا)")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "أدخل عدد الأيام")]
        [Range(1, 366, ErrorMessage = "عدد الأيام بين 1 و 366")]
        [Display(Name = "عدد الأيام")]
        public int CoverDays { get; set; } = 7;

        [Required(ErrorMessage = "اختر البند")]
        [Display(Name = "البنود")]
        public int? ClauseId { get; set; }

        [Required(ErrorMessage = "اختر البلد المزار")]
        [Display(Name = "البلد المزار")]
        public int? CountryId { get; set; }
    }

    public class TravelersPolicyVM
    {
        [Required(ErrorMessage = "يرجى إدخال اسم المسافر")]
        [Display(Name = "اسم المسافر")]
        public string TravelerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال العنوان")]
        [Display(Name = "العنوان")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم الهاتف")]
        [Display(Name = "الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم جواز السفر")]
        [Display(Name = "رقم الجواز")]
        public string PassportNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال تاريخ الميلاد")]
        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "اختر الجنس")]
        [Display(Name = "الجنس")]
        public int? GenderId { get; set; }

        [Required(ErrorMessage = "اختر الجنسية")]
        [Display(Name = "الجنسية")]
        public int? NationalityId { get; set; } = 1;

        [Required(ErrorMessage = "اختر نوع التغطية")]
        [Display(Name = "نوع التغطية")]
        public int? ClauseId { get; set; }

        /// <summary>IMS areacover package (e.g. All Schengen). Sent as both countryId and destinationId — IMS create rejects country codes like Germany.</summary>
        [Required(ErrorMessage = "اختر حزمة التغطية")]
        [Display(Name = "حزمة التغطية")]
        public int? CountryId { get; set; }

        /// <summary>Optional country label for our UI only; not sent as API destinationId.</summary>
        [Display(Name = "الدولة (اختياري)")]
        public int? DestinationId { get; set; }

        /// <summary>Resolved from CoverDays against IMS periods when available.</summary>
        public int? PeriodId { get; set; }

        [Required(ErrorMessage = "أدخل عدد الأيام")]
        [Range(1, 366, ErrorMessage = "عدد الأيام بين 1 و 366")]
        [Display(Name = "عدد الأيام")]
        public int CoverDays { get; set; } = 5;

        [Required]
        [Display(Name = "التغطية من")]
        [DataType(DataType.Date)]
        public DateTime InsuranceFrom { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "اختر تاريخ النهاية")]
        [Display(Name = "إلى")]
        [DataType(DataType.Date)]
        public DateTime InsuranceTo { get; set; } = DateTime.Today.AddDays(5);
    }
}
