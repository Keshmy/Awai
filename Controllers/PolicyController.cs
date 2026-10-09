using System.Security.Cryptography;
using Awai.Helpers;
using Awai.Models;
using Awai.Models.Entities;
using Awai.Services.Ims;
using Awai.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Awai.Controllers
{
    /// <summary>
    /// Agency policy pages. Compulsory cars calls LISA.
    /// Orange card and travel stay local demos until those APIs are wired.
    /// </summary>
    public class PolicyController : Controller
    {
        private readonly ImsApiClient _ims;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public PolicyController(ImsApiClient ims, AppDbContext db, IConfiguration config)
        {
            _ims = ims;
            _db = db;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> Compulsory()
        {
            ViewData["Title"] = "وثيقة جديدة - سيارات إجباري";
            ViewData["PolicyKind"] = PolicyProductRoutes.Compulsory;
            await LoadVehicleTypesAsync();
            return View(new CompulsoryPolicyVM());
        }

        [HttpGet]
        public async Task<IActionResult> SpecVehicles(Guid typeId)
        {
            if (!_ims.IsConfigured || typeId == Guid.Empty)
                return Json(Array.Empty<LisaSpecVehicle>());

            try
            {
                return Json(await _ims.GetSpecVehiclesAsync(typeId));
            }
            catch (ImsApiException ex)
            {
                return StatusCode((int)ex.StatusCode, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetailVehicles(Guid typeId)
        {
            if (!_ims.IsConfigured || typeId == Guid.Empty)
                return Json(Array.Empty<LisaSpecVehicle>());

            try
            {
                return Json(await _ims.GetDetailVehiclesAsync(typeId));
            }
            catch (ImsApiException ex)
            {
                return StatusCode((int)ex.StatusCode, new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewCompulsory(CompulsoryPolicyVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadVehicleTypesAsync();
                return View("Compulsory", model);
            }

            if (!_ims.IsConfigured)
                return DemoResult(model, "Compulsory", "معاينة", null);

            var rejection = DescribeLisaRejection(model);
            if (rejection != null)
            {
                ModelState.AddModelError(string.Empty, rejection);
                await LoadVehicleTypesAsync();
                return View("Compulsory", model);
            }

            try
            {
                await _ims.EnsureSessionAsync();
                var quote = await _ims.InquiryAsync(BuildLisaBody(model));
                ViewBag.DemoMode = false;
                ViewBag.ActionLabel = "معاينة القسط";
                ViewBag.BackAction = "Compulsory";
                ViewBag.ApiMessage = quote.Message;
                ViewBag.Premium = quote;
                return View("Result", model);
            }
            catch (ImsApiException ex)
            {
                var detail = ex.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? DescribeLisaRejection(model) ?? "الاحتساب مرفوض. راجع الحقول المرسلة في سجل الخادم."
                    : ex.Message;
                ModelState.AddModelError(string.Empty, detail);
                await LoadVehicleTypesAsync();
                return View("Compulsory", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueCompulsory(CompulsoryPolicyVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadVehicleTypesAsync();
                return View("Compulsory", model);
            }

            if (!_ims.IsConfigured)
            {
                var demoRef = $"DEMO-CAR-{DateTime.Now:yyyyMMddHHmmss}";
                var demo = await SaveIssuedPolicyAsync("Compulsory", model.InsuredName, model.Phone, model.PlateNumber, model.NationalId, demoRef, demoRef, null, null);
                await FillIssuedLinksAsync(demo, model.Phone);
                return DemoResult(model, "Compulsory", "إصدار", demoRef);
            }

            try
            {
                await _ims.EnsureSessionAsync();
                var issued = await _ims.CreateAsync(BuildLisaBody(model));
                var saved = await SaveIssuedPolicyAsync(
                    "Compulsory",
                    model.InsuredName,
                    model.Phone,
                    model.PlateNumber,
                    model.NationalId,
                    issued.PolicyId ?? issued.TransactionCode,
                    issued.TransactionCode,
                    issued.PdfUrl,
                    issued.Premium?.TotalPremium);

                ViewBag.DemoMode = false;
                ViewBag.ActionLabel = "إصدار الوثيقة";
                ViewBag.BackAction = "Compulsory";
                ViewBag.ApiMessage = issued.Message;
                ViewBag.PolicyRef = issued.TransactionCode ?? issued.PolicyId;
                ViewBag.PdfUrl = issued.PdfUrl;
                if (issued.Premium != null)
                {
                    ViewBag.Premium = new LisaInquiryResponse
                    {
                        Success = issued.Success,
                        Message = issued.Message,
                        NetPremium = issued.Premium.NetPremium,
                        Tax = issued.Premium.Tax,
                        SupervisionFees = issued.Premium.SupervisionFees,
                        Stamp = issued.Premium.Stamp,
                        IssuingFees = issued.Premium.IssuingFees,
                        TotalPremium = issued.Premium.TotalPremium
                    };
                }

                await FillIssuedLinksAsync(saved, model.Phone);
                return View("Result", model);
            }
            catch (ImsApiException ex)
            {
                ViewBag.DemoMode = false;
                ViewBag.IsError = true;
                ViewBag.ActionLabel = "إصدار الوثيقة";
                ViewBag.BackAction = "Compulsory";
                ViewBag.ApiMessage = ex.Message;
                return View("Result", model);
            }
        }

        /// <summary>
        /// Reopen an issued policy later (from WhatsApp or a saved link). Needs Id + AccessToken.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> View(Guid id, string? t)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(t))
                return View("NotFound");

            var policy = await _db.IssuedPolicies.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.AccessToken == t);
            if (policy == null)
                return View("NotFound");

            ViewData["Title"] = "وثيقتك";
            await FillIssuedLinksAsync(policy, policy.Phone);
            return View(policy);
        }

        private async Task<IssuedPolicy> SaveIssuedPolicyAsync(
            string productKind,
            string insuredName,
            string phone,
            string? plateNumber,
            string? nationalId,
            string? policyNumber,
            string? transactionCode,
            string? pdfUrl,
            decimal? totalPremium)
        {
            var row = new IssuedPolicy
            {
                AccessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(),
                ProductKind = productKind,
                PolicyNumber = policyNumber,
                TransactionCode = transactionCode,
                PdfUrl = pdfUrl,
                InsuredName = insuredName.Trim(),
                Phone = IssuedPolicyLinks.DigitsOnly(phone),
                PlateNumber = plateNumber?.Trim(),
                NationalId = nationalId?.Trim(),
                TotalPremium = totalPremium
            };
            _db.IssuedPolicies.Add(row);
            await _db.SaveChangesAsync();
            return row;
        }

        [HttpGet]
        public async Task<IActionResult> Pdf(Guid id, string? t)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(t))
                return NotFound();

            var policy = await _db.IssuedPolicies.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.AccessToken == t);
            if (policy == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(policy.PdfUrl)
                && (policy.PdfUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || policy.PdfUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                && !policy.PdfUrl.Contains("/Policy/Pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(policy.PdfUrl);
            }

            if (string.Equals(policy.ProductKind, "OrangeCard", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(policy.PolicyNumber)
                && _ims.IsLifoConfigured)
            {
                try
                {
                    var bytes = await _ims.GetLifoPdfAsync(policy.PolicyNumber);
                    return File(bytes, "application/pdf", $"policy_{policy.PolicyNumber}.pdf");
                }
                catch (ImsApiException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            if (string.Equals(policy.ProductKind, "Travelers", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(policy.PolicyNumber)
                && _ims.IsTravelConfigured)
            {
                try
                {
                    var bytes = await _ims.GetTravelPdfByGuidAsync(policy.PolicyNumber);
                    var fileName = policy.TransactionCode ?? policy.PolicyNumber;
                    return File(bytes, "application/pdf", $"policy_{fileName}.pdf");
                }
                catch (ImsApiException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            return NotFound();
        }

        private async Task FillIssuedLinksAsync(IssuedPolicy policy, string? customerPhone)
        {
            var viewUrl = Url.Action("View", "Policy", new { id = policy.Id, t = policy.AccessToken }, Request.Scheme)!;
            var localPdf = policy.PdfUrl
                ?? Url.Action("Pdf", "Policy", new { id = policy.Id, t = policy.AccessToken }, Request.Scheme);
            ViewBag.ViewPolicyUrl = viewUrl;
            ViewBag.PdfUrl ??= localPdf;
            ViewBag.PolicyRef ??= policy.TransactionCode ?? policy.PolicyNumber;

            // WhatsApp only clickifies public https hosts — rewrite localhost using App:PublicBaseUrl when set.
            var publicBase = _config["App:PublicBaseUrl"];
            var shareView = IssuedPolicyLinks.MakeShareableUrl(viewUrl, publicBase);
            var sharePdf = IssuedPolicyLinks.MakeShareableUrl(localPdf, publicBase);
            ViewBag.WhatsAppLinksNeedPublicHost =
                IssuedPolicyLinks.IsLocalOrNonHttpsShareUrl(sharePdf)
                || IssuedPolicyLinks.IsLocalOrNonHttpsShareUrl(shareView);

            var site = await _db.SiteInfo.AsNoTracking().FirstOrDefaultAsync();
            var companyWa = IssuedPolicyLinks.DigitsOnly(site?.WhatsAppNumber ?? site?.Phone ?? "218940001097");
            var customerWa = IssuedPolicyLinks.NormalizeLibyaWhatsApp(customerPhone ?? policy.Phone);
            var message = IssuedPolicyLinks.BuildMessage(policy, shareView, sharePdf);

            ViewBag.WhatsAppShareUrl = IssuedPolicyLinks.WhatsAppUrl(customerWa, message);
            ViewBag.WhatsAppCompanyUrl = IssuedPolicyLinks.WhatsAppUrl(companyWa, message);
            ViewBag.CustomerPhone = customerWa;
        }

        private async Task LoadVehicleTypesAsync()
        {
            ViewBag.ImsConnected = _ims.IsConfigured;
            if (!_ims.IsConfigured)
                return;

            try
            {
                ViewBag.VehicleTypes = await _ims.GetVehicleTypesAsync();
            }
            catch (ImsApiException ex)
            {
                ViewBag.ImsError = ex.Message;
                ViewBag.VehicleTypes = new List<LisaVehicleType>();
            }
        }

        private Dictionary<string, object?> BuildLisaBody(CompulsoryPolicyVM model)
        {
            var days = model.CoverDuration switch
            {
                "six" => 180,
                "three" => 90,
                _ => 364
            };
            var start = DateTime.SpecifyKind(model.StartDate.Date, DateTimeKind.Unspecified);
            var body = new Dictionary<string, object?>
            {
                ["insuredsName"] = model.InsuredName,
                ["nationality"] = model.Nationality,
                ["nidPassport"] = model.NationalId,
                ["phoneNo"] = model.Phone,
                ["address"] = "غير محدد",
                ["fromNoonOf"] = new DateTimeOffset(start, TimeSpan.FromHours(2)).ToString("yyyy-MM-dd'T'HH':'mm':'sszzz", System.Globalization.CultureInfo.InvariantCulture),
                ["typeOfVehicle"] = model.Make,
                ["plateNo"] = model.PlateNumber,
                ["chassisNo"] = model.ChassisNumber,
                ["color"] = model.Color,
                ["yearMade"] = model.ManufactureYear,
                ["loadTon"] = model.LoadTons.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["tonnage"] = model.LoadTons,
                ["regAuthority"] = string.IsNullOrWhiteSpace(model.RegistrationAuthority) ? "غير محدد" : model.RegistrationAuthority,
                ["issuanceCenter"] = _ims.BranchName,
                ["purposeLicense"] = model.Make,
                ["dayOfCarType"] = days,
                ["typeVechicleId"] = model.MainVehicleType,
                ["typeVechicle2Id"] = model.ModelDetail,
                ["issuingFeesOptions"] = 0,
                ["passengersNo"] = model.Passengers,
                ["engineHp"] = model.EngineHp,
                ["issuerName"] = _ims.IssuerName,
                ["issuerType"] = 3
            };

            if (!string.IsNullOrWhiteSpace(model.ExtraDetail) && Guid.TryParse(model.ExtraDetail, out _))
                body["typeVechicle3Id"] = model.ExtraDetail;

            return body;
        }

        /// <summary>
        /// LISA replies with the plain text "Bad Request" and no field name.
        /// These are the inputs that produce that exact reply.
        /// </summary>
        private static string? DescribeLisaRejection(CompulsoryPolicyVM model)
        {
            var notes = new List<string>();
            var libyaNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, LibyaTimeZone());
            var earliest = libyaNow.Hour >= 12 ? libyaNow.Date.AddDays(1) : libyaNow.Date;
            if (model.StartDate.Year < 2000 || model.StartDate.Date < earliest)
                notes.Add($"تاريخ البدء ({model.StartDate:yyyy-MM-dd}) يجب أن يكون {earliest:yyyy-MM-dd} أو بعده. التغطية تبدأ من الظهر، وظهر اليوم قد فات.");
            if (string.IsNullOrWhiteSpace(model.InsuredName))
                notes.Add("اسم المؤمن له فارغ");
            if (string.IsNullOrWhiteSpace(model.PlateNumber))
                notes.Add("اللوحة المعدنية فارغة");
            if (!Guid.TryParse(model.MainVehicleType, out _))
                notes.Add("اختر نوع المركبة الرئيسي من القائمة");
            if (!Guid.TryParse(model.ModelDetail, out _))
                notes.Add("اختر الموديل المحدد من القائمة");

            return notes.Count == 0 ? null : string.Join(" — ", notes);
        }

        private static TimeZoneInfo LibyaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Libya Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("Libya", TimeSpan.FromHours(2), "Libya", "Libya");
            }
        }

        [HttpGet]
        public async Task<IActionResult> OrangeCard()
        {
            ViewData["Title"] = "وثيقة جديدة - بطاقة عربية موحدة (برتقالية)";
            ViewData["PolicyKind"] = PolicyProductRoutes.OrangeCard;
            await LoadLifoLookupsAsync();
            return View(new OrangeCardPolicyVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewOrange(OrangeCardPolicyVM model)
        {
            ValidateOrangeVehicleFields(model);
            if (!ModelState.IsValid)
            {
                await LoadLifoLookupsAsync();
                return View("OrangeCard", model);
            }

            if (!_ims.IsLifoConfigured)
                return DemoResult(model, "OrangeCard", "معاينة", null);

            ViewBag.DemoMode = false;
            ViewBag.ActionLabel = "معاينة البطاقة الموحدة";
            ViewBag.BackAction = "OrangeCard";
            ViewBag.ApiMessage = "المعاينة محلية. الإصدار يرسل الطلب إلى نظام LIFO ويحفظ رقم الوثيقة.";
            ViewBag.LifoSummary = await DescribeLifoAsync(model);
            return View("Result", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueOrange(OrangeCardPolicyVM model)
        {
            ValidateOrangeVehicleFields(model);
            if (!ModelState.IsValid)
            {
                await LoadLifoLookupsAsync();
                return View("OrangeCard", model);
            }

            if (!_ims.IsLifoConfigured)
            {
                var demoRef = $"DEMO-ORG-{DateTime.Now:yyyyMMddHHmmss}";
                var demo = await SaveIssuedPolicyAsync("OrangeCard", model.InsuredName, model.Phone, model.PlateNumber, null, demoRef, demoRef, null, null);
                await FillIssuedLinksAsync(demo, model.Phone);
                return DemoResult(model, "OrangeCard", "إصدار", demoRef);
            }

            try
            {
                await _ims.EnsureLifoSessionAsync();
                var issued = await _ims.CreateLifoAsync(BuildLifoBody(model));
                var policyNo = issued.PolicyNumber ?? $"LIFO-{DateTime.Now:yyyyMMddHHmmss}";
                // Prefer IMS public document URL (https) when LIFO returns one — same idea as LISA pdfUrl.
                var publicPrint = issued.ResolveDocumentUrl();
                var hasPublicPrint = !string.IsNullOrWhiteSpace(publicPrint);

                var saved = await SaveIssuedPolicyAsync(
                    "OrangeCard",
                    model.InsuredName,
                    model.Phone,
                    model.PlateNumber,
                    null,
                    policyNo,
                    policyNo,
                    hasPublicPrint ? publicPrint : null,
                    null);

                var pdfUrl = hasPublicPrint
                    ? publicPrint!
                    : Url.Action("Pdf", "Policy", new { id = saved.Id, t = saved.AccessToken }, Request.Scheme)!;
                if (!hasPublicPrint)
                {
                    saved.PdfUrl = pdfUrl;
                    _db.IssuedPolicies.Update(saved);
                    await _db.SaveChangesAsync();
                }

                ViewBag.DemoMode = false;
                ViewBag.ActionLabel = "إصدار البطاقة الموحدة";
                ViewBag.BackAction = "OrangeCard";
                ViewBag.ApiMessage = issued.Message ?? "تم إصدار البطاقة بنجاح";
                ViewBag.PolicyRef = policyNo;
                ViewBag.PdfUrl = pdfUrl;
                ViewBag.LifoSummary = await DescribeLifoAsync(model);
                await FillIssuedLinksAsync(saved, model.Phone);
                return View("Result", model);
            }
            catch (ImsApiException ex)
            {
                ViewBag.DemoMode = false;
                ViewBag.IsError = true;
                ViewBag.ActionLabel = "إصدار البطاقة الموحدة";
                ViewBag.BackAction = "OrangeCard";
                ViewBag.ApiMessage = ex.Message;
                return View("Result", model);
            }
        }

        private async Task LoadLifoLookupsAsync()
        {
            ViewBag.LifoConnected = _ims.IsLifoConfigured;
            if (!_ims.IsLifoConfigured)
                return;

            try
            {
                await _ims.EnsureLifoSessionAsync();
                var carsTask = _ims.GetLifoCarsAsync();
                var clausesTask = _ims.GetLifoClausesAsync();
                var countriesTask = _ims.GetLifoCountriesAsync();
                var natsTask = _ims.GetLifoNationalitiesAsync();
                await Task.WhenAll(carsTask, clausesTask, countriesTask, natsTask);
                ViewBag.LifoCars = await carsTask;
                ViewBag.LifoClauses = await clausesTask;
                ViewBag.LifoCountries = await countriesTask;
                ViewBag.LifoNationalities = await natsTask;
            }
            catch (Exception ex)
            {
                ViewBag.ImsError = UnwrapImsMessage(ex);
                ViewBag.LifoCars = new List<LifoNamedItem>();
                ViewBag.LifoClauses = new List<LifoClauseItem>();
                ViewBag.LifoCountries = new List<LifoNamedItem>();
                ViewBag.LifoNationalities = new List<LifoNamedItem>();
            }
        }

        private void ValidateOrangeVehicleFields(OrangeCardPolicyVM model)
        {
            model.PlateNumber = model.PlateNumber?.Trim() ?? string.Empty;
            model.ChassisNumber = model.ChassisNumber?.Trim() ?? string.Empty;
            model.MotorNumber = model.MotorNumber?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.PlateNumber))
                ModelState.AddModelError(nameof(model.PlateNumber), "يرجى إدخال اللوحة المعدنية");
            if (string.IsNullOrWhiteSpace(model.ChassisNumber))
                ModelState.AddModelError(nameof(model.ChassisNumber), "يرجى إدخال رقم الهيكل");
            if (string.IsNullOrWhiteSpace(model.MotorNumber))
                ModelState.AddModelError(nameof(model.MotorNumber), "يرجى إدخال رقم المحرك");
            if (model.ManufactureYear is null or < 1900 or > 2050)
                ModelState.AddModelError(nameof(model.ManufactureYear), "يرجى إدخال سنة الصنع");
        }

        private static Dictionary<string, object?> BuildLifoBody(OrangeCardPolicyVM model)
        {
            return new Dictionary<string, object?>
            {
                ["insurance_name"] = model.InsuredName.Trim(),
                ["insurance_location"] = model.Address.Trim(),
                ["insurance_phone"] = model.Phone.Trim(),
                ["chassis_number"] = model.ChassisNumber.Trim(),
                ["motor_number"] = model.MotorNumber.Trim(),
                ["plate_number"] = model.PlateNumber.Trim(),
                ["car_made_date"] = model.ManufactureYear!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["cars_id"] = model.CarsId!.Value,
                ["vehicle_nationalities_id"] = model.VehicleNationalityId!.Value,
                ["insurance_day_from"] = model.StartDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["insurance_days_number"] = model.CoverDays,
                ["insurance_clauses_id"] = model.ClauseId!.Value,
                ["insurance_country_number"] = 1,
                ["countries_id"] = model.CountryId!.Value
            };
        }

        private async Task<string> DescribeLifoAsync(OrangeCardPolicyVM model)
        {
            try
            {
                var cars = ViewBag.LifoCars as IEnumerable<LifoNamedItem>;
                var clauses = ViewBag.LifoClauses as IEnumerable<LifoClauseItem>;
                var countries = ViewBag.LifoCountries as IEnumerable<LifoNamedItem>;
                var nats = ViewBag.LifoNationalities as IEnumerable<LifoNamedItem>;
                if (cars == null || clauses == null || countries == null || nats == null)
                {
                    var carsTask = _ims.GetLifoCarsAsync();
                    var clausesTask = _ims.GetLifoClausesAsync();
                    var countriesTask = _ims.GetLifoCountriesAsync();
                    var natsTask = _ims.GetLifoNationalitiesAsync();
                    await Task.WhenAll(carsTask, clausesTask, countriesTask, natsTask);
                    cars ??= await carsTask;
                    clauses ??= await clausesTask;
                    countries ??= await countriesTask;
                    nats ??= await natsTask;
                }

                var car = cars.FirstOrDefault(c => c.Id == model.CarsId)?.Name ?? model.CarsId?.ToString();
                var clause = clauses.FirstOrDefault(c => c.Id == model.ClauseId)?.Slug ?? model.ClauseId?.ToString();
                var country = countries.FirstOrDefault(c => c.Id == model.CountryId)?.Name ?? model.CountryId?.ToString();
                var nat = nats.FirstOrDefault(c => c.Id == model.VehicleNationalityId)?.Name ?? model.VehicleNationalityId?.ToString();
                var end = model.StartDate.AddDays(model.CoverDays);
                return $"المؤمن: {model.InsuredName} — اللوحة: {model.PlateNumber} — النوع: {car} — الجنسية: {nat} — البند: {clause} — البلد: {country} — من {model.StartDate:yyyy-MM-dd} لمدة {model.CoverDays} يومًا حتى {end:yyyy-MM-dd}";
            }
            catch
            {
                return $"{model.InsuredName} / {model.PlateNumber}";
            }
        }

        [HttpGet]
        public async Task<IActionResult> Travelers()
        {
            ViewData["Title"] = "وثيقة جديدة - تأمين مسافرين";
            ViewData["PolicyKind"] = PolicyProductRoutes.Travelers;
            await LoadTravelLookupsAsync();
            return View(new TravelersPolicyVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewTravel(TravelersPolicyVM model)
        {
            SyncTravelDaysFromDates(model);
            if (!ModelState.IsValid)
            {
                await LoadTravelLookupsAsync();
                return View("Travelers", model);
            }

            if (!_ims.IsTravelConfigured)
                return DemoResult(model, "Travelers", "معاينة", null);

            if (!await TryResolveTravelPeriodAsync(model))
            {
                await LoadTravelLookupsAsync();
                return View("Travelers", model);
            }

            try
            {
                await _ims.EnsureTravelSessionAsync();
                var quote = await _ims.EnquiryTravelPremiumAsync(BuildTravelEnquiryBody(model));
                ViewBag.DemoMode = false;
                ViewBag.ActionLabel = "معاينة قسط المسافرين";
                ViewBag.BackAction = "Travelers";
                ViewBag.ApiMessage = quote.Message;
                ViewBag.TravelPremium = quote;
                ViewBag.TravelSummary = await DescribeTravelAsync(model);
                if (quote.HasPremium && quote.Premium != null)
                {
                    ViewBag.Premium = new LisaInquiryResponse
                    {
                        Success = quote.Success,
                        Message = quote.Message,
                        NetPremium = ParseTravelDecimal(quote.Premium.NetPremium),
                        Tax = ParseTravelDecimal(quote.Premium.Tax),
                        SupervisionFees = ParseTravelDecimal(quote.Premium.ControlFees),
                        Stamp = ParseTravelDecimal(quote.Premium.StampFees),
                        IssuingFees = ParseTravelDecimal(quote.Premium.IssueFees),
                        TotalPremium = ParseTravelDecimal(quote.Premium.TotalPremium)
                    };
                }
                return View("Result", model);
            }
            catch (ImsApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadTravelLookupsAsync();
                return View("Travelers", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueTravel(TravelersPolicyVM model)
        {
            SyncTravelDaysFromDates(model);
            if (!ModelState.IsValid)
            {
                await LoadTravelLookupsAsync();
                return View("Travelers", model);
            }

            if (!_ims.IsTravelConfigured)
            {
                var demoRef = $"DEMO-TRV-{DateTime.Now:yyyyMMddHHmmss}";
                var demo = await SaveIssuedPolicyAsync("Travelers", model.TravelerName, model.Phone, null, model.PassportNumber, demoRef, demoRef, null, null);
                await FillIssuedLinksAsync(demo, model.Phone);
                return DemoResult(model, "Travelers", "إصدار", demoRef);
            }

            if (!await TryResolveTravelPeriodAsync(model))
            {
                await LoadTravelLookupsAsync();
                return View("Travelers", model);
            }

            try
            {
                await _ims.EnsureTravelSessionAsync();
                var quote = await _ims.EnquiryTravelPremiumAsync(BuildTravelEnquiryBody(model));
                if (!quote.HasPremium)
                {
                    ModelState.AddModelError(string.Empty,
                        string.IsNullOrWhiteSpace(quote.Message)
                            ? "IMS has no premium for this cover type / area / destination / period combination. Add travel rates in IMS, then retry."
                            : $"IMS: {quote.Message}");
                    await LoadTravelLookupsAsync();
                    return View("Travelers", model);
                }

                var issued = await _ims.CreateTravelAsync(BuildTravelCreateBody(model));
                var uniqueId = issued.UniqueId ?? issued.OrderNumber ?? issued.PolicyNumber ?? Guid.NewGuid().ToString("N");
                var displayNo = issued.PolicyNumber ?? issued.OrderNumber ?? uniqueId;
                var saved = await SaveIssuedPolicyAsync(
                    "Travelers",
                    model.TravelerName,
                    model.Phone,
                    null,
                    model.PassportNumber,
                    uniqueId,
                    displayNo,
                    null,
                    issued.Premium?.TotalPremiumValue);

                var pdfUrl = Url.Action("Pdf", "Policy", new { id = saved.Id, t = saved.AccessToken }, Request.Scheme);
                if (!string.IsNullOrWhiteSpace(issued.PdfUrl)
                    && (issued.PdfUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        || issued.PdfUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                {
                    pdfUrl = issued.PdfUrl;
                }

                saved.PdfUrl = pdfUrl;
                _db.IssuedPolicies.Update(saved);
                await _db.SaveChangesAsync();

                ViewBag.DemoMode = false;
                ViewBag.ActionLabel = "إصدار وثيقة المسافرين";
                ViewBag.BackAction = "Travelers";
                ViewBag.ApiMessage = issued.Message ?? "تم إصدار الوثيقة بنجاح";
                ViewBag.PolicyRef = displayNo;
                ViewBag.PdfUrl = pdfUrl;
                ViewBag.TravelSummary = await DescribeTravelAsync(model);
                if (issued.Premium != null)
                {
                    ViewBag.Premium = new LisaInquiryResponse
                    {
                        Success = issued.Success,
                        Message = issued.Message,
                        NetPremium = ParseTravelDecimal(issued.Premium.NetPremium),
                        Tax = ParseTravelDecimal(issued.Premium.Tax),
                        SupervisionFees = ParseTravelDecimal(issued.Premium.ControlFees),
                        Stamp = ParseTravelDecimal(issued.Premium.StampFees),
                        IssuingFees = ParseTravelDecimal(issued.Premium.IssueFees),
                        TotalPremium = ParseTravelDecimal(issued.Premium.TotalPremium)
                    };
                }
                await FillIssuedLinksAsync(saved, model.Phone);
                return View("Result", model);
            }
            catch (ImsApiException ex)
            {
                ViewBag.DemoMode = false;
                ViewBag.IsError = true;
                ViewBag.ActionLabel = "إصدار وثيقة المسافرين";
                ViewBag.BackAction = "Travelers";
                ViewBag.ApiMessage = ex.Message;
                return View("Result", model);
            }
        }

        private async Task LoadTravelLookupsAsync()
        {
            ViewBag.TravelConnected = _ims.IsTravelConfigured;
            if (!_ims.IsTravelConfigured)
                return;

            try
            {
                await _ims.EnsureTravelSessionAsync();
                var periodsTask = _ims.GetTravelPeriodsAsync();
                var gendersTask = _ims.GetTravelGendersAsync();
                var nationalitiesTask = _ims.GetTravelNationalitiesAsync();
                var destinationsTask = _ims.GetTravelDestinationsAsync();
                var coverTypesTask = _ims.GetTravelCoverTypesAsync();
                var areasTask = _ims.GetTravelAreaCoverAsync();
                await Task.WhenAll(periodsTask, gendersTask, nationalitiesTask, destinationsTask, coverTypesTask, areasTask);
                ViewBag.TravelPeriods = await periodsTask;
                ViewBag.TravelGenders = await gendersTask;
                ViewBag.TravelNationalities = await nationalitiesTask;
                ViewBag.TravelDestinations = await destinationsTask;
                ViewBag.TravelCoverTypes = await coverTypesTask;
                ViewBag.TravelAreas = await areasTask;
            }
            catch (Exception ex)
            {
                ViewBag.ImsError = UnwrapImsMessage(ex);
                ViewBag.TravelPeriods = new List<TravelNamedItem>();
                ViewBag.TravelGenders = new List<TravelNamedItem>();
                ViewBag.TravelNationalities = new List<TravelNamedItem>();
                ViewBag.TravelDestinations = new List<TravelNamedItem>();
                ViewBag.TravelCoverTypes = new List<TravelNamedItem>();
                ViewBag.TravelAreas = new List<TravelNamedItem>();
            }
        }

        private static string UnwrapImsMessage(Exception ex)
        {
            if (ex is ImsApiException ims)
                return ims.Message;
            if (ex is AggregateException agg)
            {
                var inner = agg.Flatten().InnerExceptions.OfType<ImsApiException>().FirstOrDefault();
                if (inner != null)
                    return inner.Message;
            }
            return ex.Message;
        }

        private static void SyncTravelDaysFromDates(TravelersPolicyVM model)
        {
            if (model.InsuranceTo <= model.InsuranceFrom)
                model.InsuranceTo = model.InsuranceFrom.AddDays(Math.Max(1, model.CoverDays));

            var days = (model.InsuranceTo.Date - model.InsuranceFrom.Date).Days;
            if (days >= 1)
                model.CoverDays = days;
            else if (model.CoverDays >= 1)
                model.InsuranceTo = model.InsuranceFrom.AddDays(model.CoverDays);
        }

        private async Task<bool> TryResolveTravelPeriodAsync(TravelersPolicyVM model)
        {
            var periods = (ViewBag.TravelPeriods as IList<TravelNamedItem>)
                          ?? await _ims.GetTravelPeriodsAsync();
            if (periods.Count == 0)
            {
                ModelState.AddModelError(string.Empty,
                    "نظام IMS لم يُرجع مدد التغطية (periods). التواريخ على صفحتنا تعمل، لكن احتساب القسط والإصدار يحتاج أن يضيف فريق IMS المدد في نظامهم أولاً.");
                return false;
            }

            var match = periods.FirstOrDefault(p => p.DaysCount == model.CoverDays)
                        ?? periods.FirstOrDefault(p => p.Id == model.CoverDays);
            if (match == null)
            {
                var allowed = string.Join("، ", periods
                    .Where(p => p.DaysCount is > 0)
                    .Select(p => p.DaysCount!.Value.ToString())
                    .Distinct());
                ModelState.AddModelError(nameof(model.CoverDays),
                    string.IsNullOrWhiteSpace(allowed)
                        ? $"لا توجد مدة مطابقة لـ {model.CoverDays} يوماً في IMS."
                        : $"عدد الأيام {model.CoverDays} غير معرّف في IMS. المدد المتاحة: {allowed}");
                return false;
            }

            model.PeriodId = match.Id;
            return true;
        }

        /// <summary>
        /// Live IMS accepts create only when destinationId equals the priced areacover package
        /// (e.g. Schengen id 1). countryId=package + destinationId=Germany (56) returns 400.
        /// Premium enquiry has no countryId field — send the same package id as destinationId.
        /// </summary>
        private static object BuildTravelEnquiryBody(TravelersPolicyVM model) => new
        {
            periodId = model.PeriodId,
            clauseId = model.ClauseId,
            destinationId = model.CountryId,
            insuranceFrom = model.InsuranceFrom.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            dateOfBirth = model.DateOfBirth!.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
        };

        private static object BuildTravelCreateBody(TravelersPolicyVM model)
        {
            var packageId = model.CountryId;
            return new
            {
                travelerName = model.TravelerName.Trim(),
                travelerLocation = model.Address.Trim(),
                travelerPhone = model.Phone.Trim(),
                passportNumber = model.PassportNumber.Trim(),
                dateOfBirth = model.DateOfBirth!.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                genderId = model.GenderId,
                nationalityId = model.NationalityId,
                periodId = model.PeriodId,
                clauseId = model.ClauseId,
                countryId = packageId,
                destinationId = packageId,
                insuranceFrom = model.InsuranceFrom.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            };
        }

        private static decimal ParseTravelDecimal(string? value)
            => decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0m;

        private async Task<string> DescribeTravelAsync(TravelersPolicyVM model)
        {
            try
            {
                // Prefer lists already on ViewBag; otherwise hit cached IMS lookups (no serial round-trips).
                var covers = ViewBag.TravelCoverTypes as IEnumerable<TravelNamedItem>;
                var areas = ViewBag.TravelAreas as IEnumerable<TravelNamedItem>;
                var destinations = ViewBag.TravelDestinations as IEnumerable<TravelNamedItem>;
                var periods = ViewBag.TravelPeriods as IEnumerable<TravelNamedItem>;
                if (covers == null || areas == null || periods == null)
                {
                    var coverTask = _ims.GetTravelCoverTypesAsync();
                    var areaTask = _ims.GetTravelAreaCoverAsync();
                    var destTask = _ims.GetTravelDestinationsAsync();
                    var periodTask = _ims.GetTravelPeriodsAsync();
                    await Task.WhenAll(coverTask, areaTask, destTask, periodTask);
                    covers ??= await coverTask;
                    areas ??= await areaTask;
                    destinations ??= await destTask;
                    periods ??= await periodTask;
                }

                var cover = covers.FirstOrDefault(c => c.Id == model.ClauseId)?.DisplayName;
                var package = areas.FirstOrDefault(c => c.Id == model.CountryId)?.DisplayName;
                var country = destinations?.FirstOrDefault(c => c.Id == model.DestinationId)?.DisplayName;
                var period = periods.FirstOrDefault(c => c.Id == model.PeriodId)?.DisplayName;
                return $"المسافر: {model.TravelerName} — الجواز: {model.PassportNumber} — التغطية: {cover} — الحزمة: {package}" +
                       (string.IsNullOrWhiteSpace(country) ? "" : $" — الدولة: {country}") +
                       $" — {model.CoverDays} يوماً من {model.InsuranceFrom:yyyy-MM-dd} إلى {model.InsuranceTo:yyyy-MM-dd}" +
                       (string.IsNullOrWhiteSpace(period) ? "" : $" ({period})");
            }
            catch
            {
                return $"{model.TravelerName} / {model.PassportNumber} / {model.CoverDays} يوماً";
            }
        }

        private IActionResult DemoResult(object model, string backAction, string actionLabel, string? policyRef)
        {
            if (!ModelState.IsValid)
                return View(backAction, model);

            ViewBag.DemoMode = true;
            ViewBag.ActionLabel = actionLabel;
            ViewBag.PolicyRef = policyRef;
            ViewBag.BackAction = backAction;
            ViewBag.PolicyKind = backAction;
            return View("Result", model);
        }
    }
}
