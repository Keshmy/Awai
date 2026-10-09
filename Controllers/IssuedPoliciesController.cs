using Awai.Classes;
using Awai.Helpers;
using Awai.Models;
using Awai.Models.Entities;
using Awai.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Awai.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Prog,Admin,Employee")]
    public class IssuedPoliciesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAppEmailSender _email;
        private readonly IConfiguration _config;

        public IssuedPoliciesController(AppDbContext db, IAppEmailSender email, IConfiguration config)
        {
            _db = db;
            _email = email;
            _config = config;
        }

        public async Task<IActionResult> Index(string? q)
        {
            ViewData["Title"] = "الوثائق الصادرة";
            ViewBag.Query = q;

            var query = _db.IssuedPolicies.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                var digits = IssuedPolicyLinks.DigitsOnly(term);
                query = query.Where(p =>
                    (p.InsuredName != null && p.InsuredName.Contains(term)) ||
                    (p.TransactionCode != null && p.TransactionCode.Contains(term)) ||
                    (p.PolicyNumber != null && p.PolicyNumber.Contains(term)) ||
                    (p.PlateNumber != null && p.PlateNumber.Contains(term)) ||
                    (p.NationalId != null && p.NationalId.Contains(term)) ||
                    (p.Phone != null && (p.Phone.Contains(term) || (!string.IsNullOrEmpty(digits) && p.Phone.Contains(digits)))));
            }

            var list = await query.OrderByDescending(p => p.Created).Take(200).ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> Details(Guid id, string? email)
        {
            var policy = await _db.IssuedPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (policy == null)
                return View("NotFound");

            ViewData["Title"] = "تفاصيل الوثيقة";
            FillSendLinks(policy, email);
            ViewBag.EmailConfigured = _email.IsConfigured;
            return View(policy);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmail(Guid id, string email)
        {
            var policy = await _db.IssuedPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (policy == null)
                return View("NotFound");

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "أدخل بريد الزبون أولاً";
                return RedirectToAction(nameof(Details), new { id, email });
            }

            if (!_email.IsConfigured)
            {
                TempData["ErrorMessage"] = "بريد الشركة غير مضبوط للإرسال. عيّن Smtp:UserName و Smtp:Password.";
                return RedirectToAction(nameof(Details), new { id, email });
            }

            try
            {
                var (viewUrl, pdfUrl) = ShareUrls(policy);
                var body = IssuedPolicyLinks.BuildMessage(policy, viewUrl, pdfUrl);
                var subject = $"وثيقتك — {policy.TransactionCode ?? policy.PolicyNumber}";
                await _email.SendAsync(email.Trim(), subject, body);
                TempData["SuccessMessage"] = $"تم إرسال البريد إلى {email.Trim()}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "تعذر إرسال البريد: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id, email });
        }

        private void FillSendLinks(IssuedPolicy policy, string? overrideEmail)
        {
            var (viewUrl, pdfUrl) = ShareUrls(policy);
            var message = IssuedPolicyLinks.BuildMessage(policy, viewUrl, pdfUrl);
            ViewBag.ViewPolicyUrl = viewUrl;
            ViewBag.WhatsAppUrl = IssuedPolicyLinks.WhatsAppUrl(policy.Phone, message);
            ViewBag.MailtoUrl = IssuedPolicyLinks.MailtoUrl(overrideEmail, policy, viewUrl);
            ViewBag.SendEmail = overrideEmail;
        }

        private (string viewUrl, string? pdfUrl) ShareUrls(IssuedPolicy policy)
        {
            var publicBase = _config["App:PublicBaseUrl"];
            var localView = Url.Action("View", "Policy", new { id = policy.Id, t = policy.AccessToken }, Request.Scheme)!;
            var localPdf = policy.PdfUrl
                ?? Url.Action("Pdf", "Policy", new { id = policy.Id, t = policy.AccessToken }, Request.Scheme);
            return (
                IssuedPolicyLinks.MakeShareableUrl(localView, publicBase),
                IssuedPolicyLinks.MakeShareableUrl(localPdf, publicBase));
        }
    }
}
