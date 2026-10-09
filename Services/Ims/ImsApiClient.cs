using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Awai.Services.Ims
{
    public class ImsOptions
    {
        public string BaseUrl { get; set; } = "https://awai.server.ly/IMS/";
        /// <summary>LISA compulsory-motor API key.</summary>
        public string ApiKey { get; set; } = string.Empty;
        /// <summary>LIFO orange-card API key. Falls back to ApiKey when empty.</summary>
        public string LifoApiKey { get; set; } = string.Empty;
        /// <summary>Travel insurance API key. Falls back to ApiKey when empty.</summary>
        public string TravelApiKey { get; set; } = string.Empty;
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
        private static readonly TimeSpan LookupCacheTtl = TimeSpan.FromMinutes(5);

        private readonly ImsTokenCache _lisaCache;
        private readonly ImsTokenCache _lifoCache;
        private readonly ImsTokenCache _travelCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ImsApiClient> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public ImsApiClient(
            HttpClient http,
            Microsoft.Extensions.Options.IOptions<ImsOptions> options,
            ImsTokenCache cache,
            IMemoryCache memoryCache,
            ILogger<ImsApiClient> logger)
        {
            _http = http;
            _options = options.Value;
            _lisaCache = cache;
            _lifoCache = new ImsTokenCache();
            _travelCache = new ImsTokenCache();
            _memoryCache = memoryCache;
            _logger = logger;
            if (_http.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
                _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);
        public bool IsLifoConfigured => !string.IsNullOrWhiteSpace(EffectiveLifoKey);
        public bool IsTravelConfigured => !string.IsNullOrWhiteSpace(EffectiveTravelKey);
        private string EffectiveLifoKey => !string.IsNullOrWhiteSpace(_options.LifoApiKey) ? _options.LifoApiKey : _options.ApiKey;
        private string EffectiveTravelKey => !string.IsNullOrWhiteSpace(_options.TravelApiKey) ? _options.TravelApiKey : _options.ApiKey;

        public async Task EnsureSessionAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                return;
            await GetAccessTokenAsync(_lisaCache, _options.ApiKey, cancellationToken);
        }

        public async Task EnsureLifoSessionAsync(CancellationToken cancellationToken = default)
        {
            if (!IsLifoConfigured)
                return;
            await GetAccessTokenAsync(_lifoCache, EffectiveLifoKey, cancellationToken);
        }

        public async Task EnsureTravelSessionAsync(CancellationToken cancellationToken = default)
        {
            if (!IsTravelConfigured)
                return;
            await GetAccessTokenAsync(_travelCache, EffectiveTravelKey, cancellationToken);
        }

        public Task<List<LisaVehicleType>> GetVehicleTypesAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "lisa:vehicle-types",
                async ct => await GetAsync<List<LisaVehicleType>>("api/motor/lisa/vehicle-types", ct) ?? [],
                cancellationToken);

        public Task<List<LisaSpecVehicle>> GetSpecVehiclesAsync(Guid typeId, CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                $"lisa:spec-vehicles:{typeId}",
                async ct => await GetAsync<List<LisaSpecVehicle>>($"api/motor/lisa/spec-vehicles?typeId={typeId}", ct) ?? [],
                cancellationToken);

        public Task<List<LisaSpecVehicle>> GetDetailVehiclesAsync(Guid typeId, CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                $"lisa:detail-vehicles:{typeId}",
                async ct => await GetAsync<List<LisaSpecVehicle>>($"api/motor/lisa/detail-vehicles?typeId={typeId}", ct) ?? [],
                cancellationToken);

        public async Task<LisaInquiryResponse> InquiryAsync(object body, CancellationToken cancellationToken = default)
            => await PostAsync<LisaInquiryResponse>("api/motor/lisa/inquiry", body, cancellationToken)
               ?? throw new ImsApiException(HttpStatusCode.BadGateway, "لم يرجع النظام نتيجة الاحتساب");

        public async Task<LisaCreateResponse> CreateAsync(object body, CancellationToken cancellationToken = default)
            => await PostAsync<LisaCreateResponse>("api/motor/lisa/create", body, cancellationToken)
               ?? throw new ImsApiException(HttpStatusCode.BadGateway, "لم يرجع النظام نتيجة الإصدار");

        public Task<List<LifoNamedItem>> GetLifoCarsAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "lifo:cars",
                async ct => (await GetLifoWrappedAsync<LifoNamedItem>("api/lifo/cars", ct))?.Data ?? [],
                cancellationToken);

        public Task<List<LifoClauseItem>> GetLifoClausesAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "lifo:clauses",
                async ct => (await GetLifoWrappedAsync<LifoClauseItem>("api/lifo/clauses", ct))?.Data ?? [],
                cancellationToken);

        public Task<List<LifoNamedItem>> GetLifoCountriesAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "lifo:countries",
                async ct => (await GetLifoWrappedAsync<LifoNamedItem>("api/lifo/countries", ct))?.Data ?? [],
                cancellationToken);

        public Task<List<LifoNamedItem>> GetLifoNationalitiesAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "lifo:nationalities",
                async ct => (await GetLifoWrappedAsync<LifoNamedItem>("api/lifo/nationalities", ct))?.Data ?? [],
                cancellationToken);

        public async Task<LifoCreateResponse> CreateLifoAsync(object body, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(body);
            using var response = await SendWithCacheAsync(HttpMethod.Post, "api/lifo/create", json, _lifoCache, EffectiveLifoKey, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text, "api/lifo/create", json);
            return JsonSerializer.Deserialize<LifoCreateResponse>(text, JsonOptions)
                   ?? throw new ImsApiException(HttpStatusCode.BadGateway, "لم يرجع النظام نتيجة إصدار البطاقة");
        }

        public async Task<byte[]> GetLifoPdfAsync(string policyNumber, CancellationToken cancellationToken = default)
        {
            var path = $"api/lifo/print?policyNumber={Uri.EscapeDataString(policyNumber)}";
            using var response = await SendWithCacheAsync(HttpMethod.Get, path, null, _lifoCache, EffectiveLifoKey, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                EnsureSuccess(response, text, path);
            }
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        public Task<List<TravelNamedItem>> GetTravelPeriodsAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "travel:periods",
                ct => GetTravelListAsync<TravelNamedItem>("api/travel/lookup/periods", ct),
                cancellationToken);

        public Task<List<TravelNamedItem>> GetTravelGendersAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "travel:genders",
                ct => GetTravelListAsync<TravelNamedItem>("api/travel/lookup/genders", ct),
                cancellationToken);

        public Task<List<TravelNamedItem>> GetTravelNationalitiesAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "travel:nationalities",
                ct => GetTravelListAsync<TravelNamedItem>("api/travel/lookup/nationalities", ct),
                cancellationToken);

        public Task<List<TravelNamedItem>> GetTravelDestinationsAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "travel:destinations",
                ct => GetTravelListAsync<TravelNamedItem>("api/travel/lookup/destinations", ct),
                cancellationToken);

        public Task<List<TravelNamedItem>> GetTravelCoverTypesAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "travel:coverTypes",
                ct => GetTravelListAsync<TravelNamedItem>("api/travel/lookup/coverTypes", ct),
                cancellationToken);

        public Task<List<TravelNamedItem>> GetTravelAreaCoverAsync(CancellationToken cancellationToken = default)
            => GetCachedLookupAsync(
                "travel:areacover",
                ct => GetTravelListAsync<TravelNamedItem>("api/travel/lookup/areacover", ct),
                cancellationToken);

        public async Task<TravelPremiumResponse> EnquiryTravelPremiumAsync(object body, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            using var response = await SendWithCacheAsync(HttpMethod.Post, "api/travel/premium/enquiry", json, _travelCache, EffectiveTravelKey, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text, "api/travel/premium/enquiry", json);
            return JsonSerializer.Deserialize<TravelPremiumResponse>(text, JsonOptions)
                   ?? throw new ImsApiException(HttpStatusCode.BadGateway, "لم يرجع النظام نتيجة احتساب قسط المسافرين");
        }

        public async Task<TravelCreateResponse> CreateTravelAsync(object body, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            using var response = await SendWithCacheAsync(HttpMethod.Post, "api/travel/policy/create", json, _travelCache, EffectiveTravelKey, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text, "api/travel/policy/create", json);
            return JsonSerializer.Deserialize<TravelCreateResponse>(text, JsonOptions)
                   ?? throw new ImsApiException(HttpStatusCode.BadGateway, "لم يرجع النظام نتيجة إصدار وثيقة المسافرين");
        }

        public async Task<byte[]> GetTravelPdfByGuidAsync(string uniqueId, CancellationToken cancellationToken = default)
        {
            var path = $"api/travel/policy/guid/{Uri.EscapeDataString(uniqueId)}/pdf?endNo=0&sys=27";
            using var response = await SendWithCacheAsync(HttpMethod.Get, path, null, _travelCache, EffectiveTravelKey, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                EnsureSuccess(response, text, path);
            }
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        private async Task<List<T>> GetCachedLookupAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<List<T>>> factory,
            CancellationToken cancellationToken)
        {
            var value = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = LookupCacheTtl;
                return await factory(cancellationToken);
            });
            return value ?? [];
        }

        private async Task<List<T>> GetTravelListAsync<T>(string path, CancellationToken cancellationToken)
        {
            using var response = await SendWithCacheAsync(HttpMethod.Get, path, null, _travelCache, EffectiveTravelKey, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text, path);
            if (string.IsNullOrWhiteSpace(text) || text == "[]")
                return [];
            return JsonSerializer.Deserialize<List<T>>(text, JsonOptions) ?? [];
        }

        private async Task<LifoListResponse<T>?> GetLifoWrappedAsync<T>(string path, CancellationToken cancellationToken)
        {
            using var response = await SendWithCacheAsync(HttpMethod.Get, path, null, _lifoCache, EffectiveLifoKey, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text, path);
            return JsonSerializer.Deserialize<LifoListResponse<T>>(text, JsonOptions);
        }

        private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
        {
            using var response = await SendWithCacheAsync(HttpMethod.Get, path, null, _lisaCache, _options.ApiKey, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text);
            return JsonSerializer.Deserialize<T>(text, JsonOptions);
        }

        private async Task<T?> PostAsync<T>(string path, object body, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            using var response = await SendWithCacheAsync(HttpMethod.Post, path, json, _lisaCache, _options.ApiKey, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text, path, json);
            return JsonSerializer.Deserialize<T>(text, JsonOptions);
        }

        private async Task<HttpResponseMessage> SendWithCacheAsync(
            HttpMethod method,
            string path,
            string? json,
            ImsTokenCache cache,
            string apiKey,
            CancellationToken cancellationToken)
        {
            var response = await SendOnceAsync(method, path, json, cache, apiKey, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            response.Dispose();
            await ForceRefreshAsync(cache, apiKey, cancellationToken);
            return await SendOnceAsync(method, path, json, cache, apiKey, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendOnceAsync(
            HttpMethod method,
            string path,
            string? json,
            ImsTokenCache cache,
            string apiKey,
            CancellationToken cancellationToken)
        {
            var token = await GetAccessTokenAsync(cache, apiKey, cancellationToken);
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (json != null)
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _http.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetAccessTokenAsync(ImsTokenCache cache, string apiKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ImsApiException(HttpStatusCode.Unauthorized, "مفتاح API غير مضبوط");

            if (!string.IsNullOrEmpty(cache.AccessToken) && cache.AccessExpiresUtc > DateTime.UtcNow.AddSeconds(30)
                && string.Equals(cache.ApiKeyUsed, apiKey, StringComparison.Ordinal))
                return cache.AccessToken;

            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(cache.AccessToken) && cache.AccessExpiresUtc > DateTime.UtcNow.AddSeconds(30)
                    && string.Equals(cache.ApiKeyUsed, apiKey, StringComparison.Ordinal))
                    return cache.AccessToken;

                if (!string.IsNullOrEmpty(cache.RefreshToken) && string.Equals(cache.ApiKeyUsed, apiKey, StringComparison.Ordinal))
                {
                    try
                    {
                        await RefreshCoreAsync(cache, cancellationToken);
                        return cache.AccessToken!;
                    }
                    catch (ImsApiException)
                    {
                        cache.RefreshToken = null;
                    }
                }

                await LoginCoreAsync(cache, apiKey, cancellationToken);
                return cache.AccessToken!;
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task ForceRefreshAsync(ImsTokenCache cache, string apiKey, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(cache.RefreshToken) && string.Equals(cache.ApiKeyUsed, apiKey, StringComparison.Ordinal))
                    await RefreshCoreAsync(cache, cancellationToken);
                else
                    await LoginCoreAsync(cache, apiKey, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task LoginCoreAsync(ImsTokenCache cache, string apiKey, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/token");
            request.Headers.TryAddWithoutValidation("X-API-Key", apiKey);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            using var response = await _http.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text);
            ApplyToken(cache, apiKey, text);
        }

        private async Task RefreshCoreAsync(ImsTokenCache cache, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/refresh");
            request.Headers.TryAddWithoutValidation("X-Refresh-Token", cache.RefreshToken);
            using var response = await _http.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, text);
            ApplyToken(cache, cache.ApiKeyUsed ?? string.Empty, text);
        }

        private static void ApplyToken(ImsTokenCache cache, string apiKey, string json)
        {
            var token = JsonSerializer.Deserialize<ImsTokenResponse>(json, JsonOptions)
                ?? throw new ImsApiException(HttpStatusCode.BadGateway, "استجابة تسجيل الدخول غير مفهومة");
            cache.AccessToken = token.AccessToken;
            cache.RefreshToken = token.RefreshToken;
            cache.AccessExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 30));
            cache.IssuerName = token.FullName;
            cache.BranchName = token.BranchName;
            cache.ApiKeyUsed = apiKey;
        }

        public string IssuerName => string.IsNullOrWhiteSpace(_lisaCache.IssuerName) ? "الواحة العربية" : _lisaCache.IssuerName;
        public string BranchName => string.IsNullOrWhiteSpace(_lisaCache.BranchName) ? "الواحة العربية للتأمين" : _lisaCache.BranchName;

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
            else if (response.StatusCode == HttpStatusCode.BadRequest
                     && (string.IsNullOrWhiteSpace(message)
                         || message.Equals("Bad Request", StringComparison.OrdinalIgnoreCase)))
                message = "IMS rejected the request (400). Usually no premium rate exists for this period / cover / area / destination combination.";
            else if (string.IsNullOrWhiteSpace(message) || message.TrimStart().StartsWith("<", StringComparison.Ordinal))
                message = response.StatusCode == HttpStatusCode.Unauthorized
                    ? "نظام التأمين رفض تسجيل الدخول. تحقق من مفتاح API."
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
        public string? ApiKeyUsed { get; set; }
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

    public class LifoListResponse<T>
    {
        public bool Status { get; set; }
        public string? Message { get; set; }
        public List<T>? Data { get; set; }
    }

    public class LifoNamedItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Symbol { get; set; }
    }

    public class LifoClauseItem
    {
        public int Id { get; set; }
        public string? Slug { get; set; }
        public string? Type { get; set; }
        [JsonPropertyName("type_id")]
        public string? TypeId { get; set; }
    }

    public class LifoCreateResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? PolicyNumber { get; set; }
        [JsonPropertyName("printUrl")]
        public string? PrintUrl { get; set; }
        [JsonPropertyName("pdfUrl")]
        public string? PdfUrl { get; set; }
        public JsonElement? Data { get; set; }

        /// <summary>Best public/document URL from create payload (LISA-style https when IMS provides it).</summary>
        public string? ResolveDocumentUrl()
        {
            foreach (var candidate in new[] { PrintUrl, PdfUrl })
            {
                if (!string.IsNullOrWhiteSpace(candidate)
                    && (candidate.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        || candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                    return candidate.Trim();
            }

            if (Data is { ValueKind: JsonValueKind.Object } data)
            {
                foreach (var name in new[] { "printUrl", "PrintUrl", "pdfUrl", "PdfUrl", "url", "Url" })
                {
                    if (data.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String)
                    {
                        var s = el.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(s)
                            && (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                            return s;
                    }
                }
            }

            return null;
        }
    }

    public class TravelNamedItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? DaysCount { get; set; }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return string.Empty;
                var parts = Name.Split("||", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var arabic = parts.FirstOrDefault(p => p.Any(c => c > 127));
                    return arabic ?? parts[^1];
                }
                return Name.Trim();
            }
        }
    }

    public class TravelPremiumResponse
    {
        public bool Success { get; set; }
        public bool HasPremium { get; set; }
        public string? Message { get; set; }
        public TravelPremiumBlock? Premium { get; set; }
    }

    public class TravelPremiumBlock
    {
        public string? NetPremium { get; set; }
        public string? Tax { get; set; }
        public string? ControlFees { get; set; }
        public string? StampFees { get; set; }
        public string? IssueFees { get; set; }
        public string? TotalPremium { get; set; }

        public decimal? TotalPremiumValue
            => decimal.TryParse(TotalPremium, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public class TravelCreateResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? PolicyNumber { get; set; }
        public string? OrderNumber { get; set; }
        public string? UniqueId { get; set; }
        public string? PdfUrl { get; set; }
        public TravelPremiumBlock? Premium { get; set; }
    }
}
