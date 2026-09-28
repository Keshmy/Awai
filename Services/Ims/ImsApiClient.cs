using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Awai.Services.Ims
{
    public class ImsOptions
    {
        public string BaseUrl { get; set; } = "https://awai.server.ly/IMS/";
        public string ApiKey { get; set; } = string.Empty;
    }

    public class ImsApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public ImsApiException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class ImsApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly HttpClient _http;
        private readonly ImsOptions _options;
        private readonly ImsTokenCache _cache;
        private readonly ILogger<ImsApiClient> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public ImsApiClient(HttpClient http, Microsoft.Extensions.Options.IOptions<ImsOptions> options, ImsTokenCache cache, ILogger<ImsApiClient> logger)
        {
            _http = http;
            _options = options.Value;
            _cache = cache;
            _logger = logger;
            if (_http.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
                _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

        public async Task EnsureSessionAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                return;
            await GetAccessTokenAsync(cancellationToken);
        }

        public async Task<List<LisaVehicleType>> GetVehicleTypesAsync(CancellationToken cancellationToken = default)
            => await GetAsync<List<LisaVehicleType>>("api/motor/lisa/vehicle-types", cancellationToken) ?? [];

        public async Task<List<LisaSpecVehicle>> GetSpecVehiclesAsync(Guid typeId, CancellationToken cancellationToken = default)
            => await GetAsync<List<LisaSpecVehicle>>($"api/motor/lisa/spec-vehicles?typeId={typeId}", cancellationToken) ?? [];

        public async Task<List<LisaSpecVehicle>> GetDetailVehiclesAsync(Guid typeId, CancellationToken cancellationToken = default)
            => await GetAsync<List<LisaSpecVehicle>>($"api/motor/lisa/detail-vehicles?typeId={typeId}", cancellationToken) ?? [];

        public async Task<LisaInquiryResponse> InquiryAsync(object body, CancellationToken cancellationToken = default)
            => await PostAsync<LisaInquiryResponse>("api/motor/lisa/inquiry", body, cancellationToken)
               ?? throw new ImsApiException(HttpStatusCode.BadGateway, "لم يرجع النظام نتيجة الاحتساب");

        public async Task<LisaCreateResponse> CreateAsync(object body, CancellationToken cancellationToken = default)
            => await PostAsync<LisaCreateResponse>("api/motor/lisa/create", body, cancellationToken)
               ?? throw new ImsApiException(HttpStatusCode.BadGateway, "لم يرجع النظام نتيجة الإصدار");

        private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
        {
            using var response = await SendAsync(HttpMethod.Get, path, null, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text);
            return JsonSerializer.Deserialize<T>(text, JsonOptions);
        }

        private async Task<T?> PostAsync<T>(string path, object body, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            using var response = await SendAsync(HttpMethod.Post, path, json, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text, path, json);
            return JsonSerializer.Deserialize<T>(text, JsonOptions);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? json, CancellationToken cancellationToken)
        {
            var response = await SendOnceAsync(method, path, json, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            response.Dispose();
            await ForceRefreshAsync(cancellationToken);
            return await SendOnceAsync(method, path, json, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendOnceAsync(HttpMethod method, string path, string? json, CancellationToken cancellationToken)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (json != null)
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _http.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(_cache.AccessToken) && _cache.AccessExpiresUtc > DateTime.UtcNow.AddSeconds(30))
                return _cache.AccessToken;

            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(_cache.AccessToken) && _cache.AccessExpiresUtc > DateTime.UtcNow.AddSeconds(30))
                    return _cache.AccessToken;

                if (!string.IsNullOrEmpty(_cache.RefreshToken))
                {
                    try
                    {
                        await RefreshCoreAsync(cancellationToken);
                        return _cache.AccessToken!;
                    }
                    catch (ImsApiException)
                    {
                        _cache.RefreshToken = null;
                    }
                }

                await LoginCoreAsync(cancellationToken);
                return _cache.AccessToken!;
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task ForceRefreshAsync(CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(_cache.RefreshToken))
                    await RefreshCoreAsync(cancellationToken);
                else
                    await LoginCoreAsync(cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task LoginCoreAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/token");
            request.Headers.TryAddWithoutValidation("X-API-Key", _options.ApiKey);
            using var response = await _http.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text);
            ApplyToken(text);
        }

        private async Task RefreshCoreAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/refresh");
            request.Headers.TryAddWithoutValidation("X-Refresh-Token", _cache.RefreshToken);
            using var response = await _http.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text);
            ApplyToken(text);
        }

        private void ApplyToken(string json)
        {
            var token = JsonSerializer.Deserialize<ImsTokenResponse>(json, JsonOptions)
                ?? throw new ImsApiException(HttpStatusCode.BadGateway, "استجابة تسجيل الدخول غير مفهومة");
            _cache.AccessToken = token.AccessToken;
            _cache.RefreshToken = token.RefreshToken;
            _cache.AccessExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 30));
            _cache.IssuerName = token.FullName;
            _cache.BranchName = token.BranchName;
        }

        public string IssuerName => string.IsNullOrWhiteSpace(_cache.IssuerName) ? "الواحة العربية" : _cache.IssuerName;
        public string BranchName => string.IsNullOrWhiteSpace(_cache.BranchName) ? "الواحة العربية للتأمين" : _cache.BranchName;

        private void EnsureSuccess(HttpResponseMessage response, string body, string? path = null, string? requestJson = null)
        {
            if (response.IsSuccessStatusCode)
                return;

            _logger.LogWarning("IMS {Path} returned {Status}. Response: {Body}. Request: {Request}", path ?? response.RequestMessage?.RequestUri?.AbsolutePath, (int)response.StatusCode, body, requestJson);

            var message = body;
            try
            {
                var err = JsonSerializer.Deserialize<ImsErrorResponse>(body, JsonOptions);
                if (!string.IsNullOrWhiteSpace(err?.Error))
                    message = err.Error;
            }
            catch
            {
                // keep raw body
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
                message = "وضع الاختبار لا يسمح بإصدار الوثيقة. احتساب القسط يعمل، والإصدار يحتاج مفتاح Production.";
            else if (string.IsNullOrWhiteSpace(message) || message.TrimStart().StartsWith("<", StringComparison.Ordinal))
                message = response.StatusCode == HttpStatusCode.Unauthorized
                    ? "نظام التأمين رفض تسجيل الدخول. مسار المفتاح ما زال يطلب جلسة الموقع ولا يقبل مفتاح API."
                    : $"نظام التأمين أعاد صفحة بدل JSON ({(int)response.StatusCode}).";

            throw new ImsApiException(response.StatusCode, message);
        }
    }

    public class ImsTokenCache
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime AccessExpiresUtc { get; set; }
        public string? IssuerName { get; set; }
        public string? BranchName { get; set; }
    }

    public class ImsTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string? FullName { get; set; }
        public string? BranchName { get; set; }
    }

    public class ImsErrorResponse
    {
        public string? Error { get; set; }
        public string? Code { get; set; }
    }

    public class LisaVehicleType
    {
        public Guid Id { get; set; }
        public string TypeVehicle { get; set; } = string.Empty;
        public int Number { get; set; }
    }

    public class LisaSpecVehicle
    {
        public Guid Id { get; set; }
        public string SpecVehicle { get; set; } = string.Empty;
        public decimal PremiumYear { get; set; }
    }

    public class LisaInquiryResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public decimal NetPremium { get; set; }
        public decimal Tax { get; set; }
        public decimal SupervisionFees { get; set; }
        public decimal Stamp { get; set; }
        public decimal IssuingFees { get; set; }
        public decimal TotalPremium { get; set; }
        public DateTime? ToNoonOf { get; set; }
    }

    public class LisaCreateResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? PolicyId { get; set; }
        public string? TransactionCode { get; set; }
        public string? PdfUrl { get; set; }
        public LisaPremiumBlock? Premium { get; set; }
    }

    public class LisaPremiumBlock
    {
        public decimal NetPremium { get; set; }
        public decimal Tax { get; set; }
        public decimal SupervisionFees { get; set; }
        public decimal Stamp { get; set; }
        public decimal IssuingFees { get; set; }
        public decimal TotalPremium { get; set; }
    }
}
