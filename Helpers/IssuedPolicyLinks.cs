using Awai.Models.Entities;

namespace Awai.Helpers
{
    public static class IssuedPolicyLinks
    {
        public static string DigitsOnly(string? value)
            => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

        public static string NormalizeLibyaWhatsApp(string? phone)
        {
            var digits = DigitsOnly(phone);
            if (string.IsNullOrEmpty(digits))
                return string.Empty;
            if (digits.StartsWith("218"))
                return digits;
            if (digits.StartsWith('0'))
                return "218" + digits.TrimStart('0');
            if (digits.Length == 9)
                return "218" + digits;
            return digits;
        }

        public static string BuildMessage(IssuedPolicy policy, string viewUrl, string? pdfUrl = null)
        {
            var kind = policy.ProductKind switch
            {
                "OrangeCard" => "بطاقة عربية موحدة (برتقالية)",
                "Travelers" => "تأمين مسافرين",
                _ => "تأمين سيارات إجباري"
            };

            // Put each URL alone on its line so WhatsApp linkifies it (Arabic + URL on one line often stays plain text).
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"وثيقة {kind}");
            sb.AppendLine($"الاسم: {policy.InsuredName}");
            sb.AppendLine($"الهاتف: {NormalizeLibyaWhatsApp(policy.Phone)}");
            if (!string.IsNullOrWhiteSpace(policy.PlateNumber))
                sb.AppendLine($"اللوحة: {policy.PlateNumber}");
            sb.AppendLine($"رقم المعاملة: {policy.TransactionCode ?? policy.PolicyNumber}");
            sb.AppendLine("رابط الوثيقة على الموقع:");
            sb.AppendLine(viewUrl.Trim());

            var pdf = PdfUrlForWhatsApp(pdfUrl ?? policy.PdfUrl, viewUrl);
            if (!string.IsNullOrWhiteSpace(pdf))
            {
                sb.AppendLine("ملف الوثيقة:");
                sb.Append(pdf);
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// WhatsApp only auto-linkifies public hosts (especially https). localhost stays plain text.
        /// Rewrite local URLs to App:PublicBaseUrl when configured.
        /// </summary>
        public static string MakeShareableUrl(string? url, string? publicBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            var trimmed = url.Trim();
            if (string.IsNullOrWhiteSpace(publicBaseUrl))
                return trimmed;

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
                return trimmed;

            var isLocal = absolute.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                          || absolute.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
            if (!isLocal)
                return trimmed;

            var baseUrl = publicBaseUrl.Trim().TrimEnd('/');
            return baseUrl + absolute.PathAndQuery;
        }

        public static bool IsLocalOrNonHttpsShareUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u))
                return true;
            if (u.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || u.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                return true;
            return !u.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Include any absolute PDF URL. Skip only when missing or identical to the view link.
        /// </summary>
        public static string? PdfUrlForWhatsApp(string? pdfUrl, string? viewUrl = null)
        {
            if (string.IsNullOrWhiteSpace(pdfUrl))
                return null;

            var pdf = pdfUrl.Trim();
            if (!string.IsNullOrWhiteSpace(viewUrl)
                && string.Equals(pdf, viewUrl.Trim(), StringComparison.OrdinalIgnoreCase))
                return null;

            if (pdf.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || pdf.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                return pdf;

            return null;
        }

        public static string? WhatsAppUrl(string? phone, string message)
        {
            var wa = NormalizeLibyaWhatsApp(phone);
            if (string.IsNullOrEmpty(wa))
                return null;
            return $"https://wa.me/{wa}?text={Uri.EscapeDataString(message)}";
        }

        public static string? MailtoUrl(string? email, IssuedPolicy policy, string viewUrl)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;
            var subject = Uri.EscapeDataString($"وثيقتك — {policy.TransactionCode ?? policy.PolicyNumber}");
            var body = Uri.EscapeDataString(BuildMessage(policy, viewUrl));
            return $"mailto:{email.Trim()}?subject={subject}&body={body}";
        }
    }
}
