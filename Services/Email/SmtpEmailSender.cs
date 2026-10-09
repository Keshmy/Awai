namespace Awai.Services.Email
{
    public class SmtpOptions
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string FromName { get; set; } = "الواحة العربية للتأمين";
    }

    public interface IAppEmailSender
    {
        bool IsConfigured { get; }
        Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    }

    public class SmtpEmailSender : IAppEmailSender
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(Microsoft.Extensions.Options.IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured
            => !string.IsNullOrWhiteSpace(_options.Host)
               && !string.IsNullOrWhiteSpace(_options.UserName)
               && !string.IsNullOrWhiteSpace(_options.Password);

        public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("إعدادات البريد غير مكتملة. عيّن Smtp:UserName و Smtp:Password في الأسرار أو متغيرات البيئة.");

            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("عنوان المستلم فارغ", nameof(to));

            var from = string.IsNullOrWhiteSpace(_options.From) ? _options.UserName : _options.From;
            using var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(from, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(to.Trim());

            using var client = new System.Net.Mail.SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(_options.UserName, _options.Password)
            };

            // SmtpClient.SendMailAsync does not accept CancellationToken on all TFMs.
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}", to);
        }
    }
}
