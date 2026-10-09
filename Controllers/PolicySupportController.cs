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
    public class PolicySupportController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAppEmailSender _email;
        private readonly IConfiguration _config;

        public PolicySupportController(AppDbContext db, IAppEmailSender email, IConfiguration config)
        {
            _db = db;
            _email = email;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? status)
        {
            ViewData["Title"] = "طلبات استرجاع الوثائق";
            ViewBag.Status = status;

            var query = _db.PolicySupportRequests.AsNoTracking().AsQueryable();
            if (string.Equals(status, "open", StringComparison.OrdinalIgnoreCase))
                query = query.Where(r => !r.IsHandled);
            else if (string.Equals(status, "done", StringComparison.OrdinalIgnoreCase))
                query = query.Where(r => r.IsHandled);

            var list = await query.OrderByDescending(r => r.Created).Take(200).ToListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var request = await _db.PolicySupportRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
                return View("NotFound");

            ViewData["Title"] = "طلب استرجاع وثيقة";
            IssuedPolicy? match = null;
            if (request.MatchedIssuedPolicyId.HasValue)
            {
                match = await _db.IssuedPolicies.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.MatchedIssuedPolicyId);
            }

            if (match == null)
            {
                var phoneDigits = IssuedPolicyLinks.DigitsOnly(request.Phone);
                var phoneTail = phoneDigits.Length >= 9 ? phoneDigits.Substring(phoneDigits.Length - 9) : phoneDigits;
                match = await _db.IssuedPolicies.AsNoTracking()
                    .Where(p =>
                        (!string.IsNullOrEmpty(request.PolicyNumber) && (p.TransactionCode == request.PolicyNumber || p.PolicyNumber == request.PolicyNumber)) ||
                        (!string.IsNullOrEmpty(request.NationalId) && p.NationalId == request.NationalId) ||
                        (!string.IsNullOrEmpty(phoneTail) && p.Phone != null && p.Phone.Contains(phoneTail)))
                    .OrderByDescending(p => p.Created)
                    .FirstOrDefaultAsync();
            }

            if (match != null)
            {
                var (viewUrl, pdfUrl) = ShareUrls(match);
                var message = IssuedPolicyLinks.BuildMessage(match, viewUrl, pdfUrl);
                ViewBag.MatchedPolicy = match;
                ViewBag.ViewPolicyUrl = viewUrl;
                ViewBag.WhatsAppUrl = IssuedPolicyLinks.WhatsAppUrl(request.Phone, message);
                ViewBag.MailtoUrl = IssuedPolicyLinks.MailtoUrl(request.Email, match, viewUrl);
            }

            ViewBag.EmailConfigured = _email.IsConfigured;
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmail(Guid id)
        {
            var request = await _db.PolicySupportRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
                return View("NotFound");

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                TempData["ErrorMessage"] = "الطلب لا يحتوي على بريد إلكتروني";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!_email.IsConfigured)
            {
                TempData["ErrorMessage"] = "بريد الشركة غير مضبوط للإرسال. عيّن Smtp:UserName و Smtp:Password.";
                return RedirectToAction(nameof(Details), new { id });
            }

            IssuedPolicy? match = null;
            if (request.MatchedIssuedPolicyId.HasValue)
                match = await _db.IssuedPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.MatchedIssuedPolicyId);

            if (match == null)
            {
                TempData["ErrorMessage"] = "لا توجد وثيقة مطابقة لإرسالها";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var (viewUrl, pdfUrl) = ShareUrls(match);
                var body = IssuedPolicyLinks.BuildMessage(match, viewUrl, pdfUrl);
                var subject = $"وثيقتك — {match.TransactionCode ?? match.PolicyNumber}";
                await _email.SendAsync(request.Email.Trim(), subject, body);
                TempData["SuccessMessage"] = $"تم إرسال البريد إلى {request.Email.Trim()}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "تعذر إرسال البريد: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkHandled(Guid id)
        {
            var request = await _db.PolicySupportRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
                return View("NotFound");

            request.IsHandled = true;
            request.Modified = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "تم تعليم الطلب كمُنجز";
            return RedirectToAction(nameof(Details), new { id });
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
