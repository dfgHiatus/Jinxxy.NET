using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Jinxxy.Models;

namespace Jinxxy;

/// <summary>
/// Client for the Jinxxy Creator API v1 (<c>https://api.creators.jinxxy.com/v1</c>).
/// Authenticates with an API key created in the Jinxxy creator dashboard.
/// Thread-safe; create one per API key and reuse it.
/// </summary>
public sealed class JinxxyClient : IDisposable
{
    private const string ApiKeyHeader = "x-api-key";
    private static readonly HttpMethod Patch = new("PATCH");

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly JinxxyClientOptions _options;
    private readonly string _apiKey;
    private static JinxxyJsonContext Json => JinxxyJsonContext.Default;

    /// <param name="apiKey">API key from the Jinxxy creator dashboard.</param>
    /// <param name="httpClient">Optional shared HttpClient. It is not disposed by this client.</param>
    /// <param name="options">Optional behaviour overrides.</param>
    public JinxxyClient(string apiKey, HttpClient? httpClient = null, JinxxyClientOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("An API key is required.", nameof(apiKey));

        _apiKey = apiKey.Trim();
        _options = options ?? new JinxxyClientOptions();
        _ownsHttpClient = httpClient is null;
        _http = httpClient ?? new HttpClient();
    }

    /// <summary>Rate limit headers from the most recent response, if any.</summary>
    public RateLimitInfo? LastRateLimit { get; private set; }

    // ---------------------------------------------------------------- User

    /// <summary><c>GET /me</c>: the API key owner and the scopes granted to the key.</summary>
    public Task<CurrentUser> GetMeAsync(CancellationToken ct = default) =>
        GetAsync("me", Json.CurrentUser, ct);

    // ------------------------------------------------------------ Products

    /// <summary><c>GET /products</c> (scope <c>products_read</c>).</summary>
    public Task<PagedResult<ProductSummary>> ListProductsAsync(ListQuery? query = null, CancellationToken ct = default) =>
        GetAsync("products" + BuildQuery(query), Json.PagedResultProductSummary, ct);

    /// <summary>Enumerates every product across all pages.</summary>
    public IAsyncEnumerable<ProductSummary> EnumerateProductsAsync(ListQuery? query = null, CancellationToken ct = default) =>
        EnumerateAsync(query ?? new ListQuery(), ListProductsAsync, ct);

    /// <summary><c>GET /products/{id}</c> (scope <c>products_read</c>).</summary>
    public Task<Product> GetProductAsync(string id, CancellationToken ct = default) =>
        GetAsync("products/" + Escape(id), Json.Product, ct);

    // -------------------------------------------------------------- Orders

    /// <summary><c>GET /orders</c> (scope <c>orders_read</c>).</summary>
    public Task<PagedResult<OrderSummary>> ListOrdersAsync(ListQuery? query = null, CancellationToken ct = default) =>
        GetAsync("orders" + BuildQuery(query), Json.PagedResultOrderSummary, ct);

    public IAsyncEnumerable<OrderSummary> EnumerateOrdersAsync(ListQuery? query = null, CancellationToken ct = default) =>
        EnumerateAsync(query ?? new ListQuery(), ListOrdersAsync, ct);

    /// <summary><c>GET /orders/{id}</c> (scope <c>orders_read</c>), including order items and licenses.</summary>
    public Task<Order> GetOrderAsync(string id, CancellationToken ct = default) =>
        GetAsync("orders/" + Escape(id), Json.Order, ct);

    // ------------------------------------------------------------ Licenses

    /// <summary><c>GET /licenses</c> (scope <c>licenses_read</c>).</summary>
    public Task<PagedResult<LicenseSummary>> ListLicensesAsync(LicenseListQuery? query = null, CancellationToken ct = default) =>
        GetAsync("licenses" + BuildQuery(query), Json.PagedResultLicenseSummary, ct);

    /// <summary><c>GET /licenses/{id}</c> (scope <c>licenses_read</c>).</summary>
    public Task<License> GetLicenseAsync(string id, CancellationToken ct = default) =>
        GetAsync("licenses/" + Escape(id), Json.License, ct);

    /// <summary><c>GET /licenses/{id}/activations</c> (scope <c>licenses_read</c>).</summary>
    public Task<LicenseActivationPage> ListLicenseActivationsAsync(string licenseId, ListQuery? query = null, CancellationToken ct = default) =>
        GetAsync($"licenses/{Escape(licenseId)}/activations" + BuildQuery(query), Json.LicenseActivationPage, ct);

    /// <summary><c>GET /licenses/{id}/activations/{activation_id}</c> (scope <c>licenses_read</c>).</summary>
    public Task<LicenseActivation> GetLicenseActivationAsync(string licenseId, string activationId, CancellationToken ct = default) =>
        GetAsync($"licenses/{Escape(licenseId)}/activations/{Escape(activationId)}", Json.LicenseActivation, ct);

    /// <summary><c>POST /licenses/{id}/activations</c> (scope <c>licenses_write</c>).</summary>
    public Task<LicenseActivation> CreateLicenseActivationAsync(string licenseId, string description, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(description))
            throw new ArgumentException("Description must not be empty.", nameof(description));

        var body = JsonSerializer.Serialize(new CreateLicenseActivationRequest { Description = description }, Json.CreateLicenseActivationRequest);
        return SendAsync(HttpMethod.Post, $"licenses/{Escape(licenseId)}/activations", body, Json.LicenseActivation, ct);
    }

    /// <summary><c>DELETE /licenses/{id}/activations/{activation_id}</c> (scope <c>licenses_write</c>).</summary>
    public Task<DeleteActivationResult> DeleteLicenseActivationAsync(string licenseId, string activationId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"licenses/{Escape(licenseId)}/activations/{Escape(activationId)}", null, Json.DeleteActivationResult, ct);

    // ----------------------------------------------------------- Customers

    /// <summary><c>GET /customers</c> (scope <c>customers_read</c>).</summary>
    public Task<PagedResult<Customer>> ListCustomersAsync(ListQuery? query = null, CancellationToken ct = default) =>
        GetAsync("customers" + BuildQuery(query), Json.PagedResultCustomer, ct);

    public IAsyncEnumerable<Customer> EnumerateCustomersAsync(ListQuery? query = null, CancellationToken ct = default) =>
        EnumerateAsync(query ?? new ListQuery(), ListCustomersAsync, ct);

    /// <summary><c>GET /customers/{id}</c> (scope <c>customers_read</c>).</summary>
    public Task<Customer> GetCustomerAsync(string id, CancellationToken ct = default) =>
        GetAsync("customers/" + Escape(id), Json.Customer, ct);

    // ------------------------------------------------------ Discount codes

    /// <summary><c>GET /discount_codes</c> (scope <c>discount_codes_read</c>).</summary>
    public Task<PagedResult<DiscountCode>> ListDiscountCodesAsync(ListQuery? query = null, CancellationToken ct = default) =>
        GetAsync("discount_codes" + BuildQuery(query), Json.PagedResultDiscountCode, ct);

    /// <summary><c>GET /discount_codes/{id}</c> (scope <c>discount_codes_read</c>).</summary>
    public Task<DiscountCode> GetDiscountCodeAsync(string id, CancellationToken ct = default) =>
        GetAsync("discount_codes/" + Escape(id), Json.DiscountCode, ct);

    /// <summary><c>POST /discount_codes</c> (scope <c>discount_codes_write</c>).</summary>
    public Task<DiscountCode> CreateDiscountCodeAsync(CreateDiscountCodeRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        var body = JsonSerializer.Serialize(request, Json.CreateDiscountCodeRequest);
        return SendAsync(HttpMethod.Post, "discount_codes", body, Json.DiscountCode, ct);
    }

    /// <summary><c>PATCH /discount_codes/{id}</c> (scope <c>discount_codes_write</c>).</summary>
    public Task<DiscountCode> UpdateDiscountCodeAsync(string id, UpdateDiscountCodeRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        var node = JsonSerializer.SerializeToNode(request, Json.UpdateDiscountCodeRequest)!.AsObject();
        if (request.ClearUseLimit) node["use_limit"] = null;
        if (request.ClearExpiresAt) node["expires_at"] = null;
        return SendAsync(Patch, "discount_codes/" + Escape(id), node.ToJsonString(), Json.DiscountCode, ct);
    }

    /// <summary><c>DELETE /discount_codes/{id}</c> (scope <c>discount_codes_write</c>).</summary>
    public Task<DeleteDiscountCodeResult> DeleteDiscountCodeAsync(string id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, "discount_codes/" + Escape(id), null, Json.DeleteDiscountCodeResult, ct);

    // ------------------------------------------------------------ Plumbing

    private static async IAsyncEnumerable<T> EnumerateAsync<T>(
        ListQuery query,
        Func<ListQuery, CancellationToken, Task<PagedResult<T>>> fetch,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var page = query.Page ?? 1;
        while (true)
        {
            var result = await fetch(query with { Page = page }, ct).ConfigureAwait(false);
            foreach (var item in result.Results)
                yield return item;

            if (result.Results.Count == 0 || result.Page >= result.PageCount)
                yield break;
            page = result.Page + 1;
        }
    }

    private Task<T> GetAsync<T>(string path, JsonTypeInfo<T> type, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, path, null, type, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, string? jsonBody, JsonTypeInfo<T> type, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, new Uri(_options.BaseAddress, path));
            request.Headers.Add(ApiKeyHeader, _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
            if (jsonBody is not null)
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            LastRateLimit = ReadRateLimit(response) ?? LastRateLimit;

            if (response.IsSuccessStatusCode)
            {
#if NETSTANDARD2_0
                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
                await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
#endif
                return await JsonSerializer.DeserializeAsync(stream, type, ct).ConfigureAwait(false)
                       ?? throw new JinxxyApiException(response.StatusCode, null, ReadRequestId(response), LastRateLimit);
            }

            var error = await ReadErrorAsync(response).ConfigureAwait(false);
            if ((int)response.StatusCode == 429 && _options.RetryOnRateLimit && attempt < _options.MaxRateLimitRetries)
            {
                await Task.Delay(GetRateLimitDelay(response), ct).ConfigureAwait(false);
                continue;
            }

            throw new JinxxyApiException(response.StatusCode, error, ReadRequestId(response), LastRateLimit);
        }
    }

    private TimeSpan GetRateLimitDelay(HttpResponseMessage response)
    {
        TimeSpan delay;
        if (response.Headers.RetryAfter?.Delta is { } delta)
            delay = delta;
        else if (response.Headers.RetryAfter?.Date is { } date)
            delay = date - DateTimeOffset.UtcNow;
        else if (LastRateLimit?.ResetsAt is { } reset)
            delay = reset - DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(250);
        else
            delay = TimeSpan.FromSeconds(5);

        if (delay < TimeSpan.FromMilliseconds(250)) delay = TimeSpan.FromMilliseconds(250);
        return delay > _options.MaxRateLimitWait ? _options.MaxRateLimitWait : delay;
    }

    private static async Task<ApiError?> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(text)) return null;
            return JsonSerializer.Deserialize(text, Json.ApiError);
        }
        catch (JsonException) { return null; }
        catch (IOException) { return null; }
    }

    internal static RateLimitInfo? ReadRateLimit(HttpResponseMessage response)
    {
        var limit = ReadIntHeader(response, "X-RateLimit-Limit");
        var remaining = ReadIntHeader(response, "X-RateLimit-Remaining");
        var reset = ReadIntHeader(response, "X-RateLimit-Reset");
        if (limit is null && remaining is null && reset is null) return null;
        return new RateLimitInfo(limit, remaining, reset is { } r ? DateTimeOffset.FromUnixTimeSeconds(r) : null);
    }

    private static int? ReadIntHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values)
        && int.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
            ? v
            : null;

    private static string? ReadRequestId(HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-Request-ID", out var values) ? values.FirstOrDefault() : null;

    private static string Escape(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID must not be empty.", nameof(id));
        return Uri.EscapeDataString(id);
    }

    internal static string BuildQuery(ListQuery? q) =>
        q is null ? "" : BuildQuery(q.Page, q.Limit, q.SearchQuery, q.SortField switch
        {
            ListSortField.CreatedAt => "created_at",
            ListSortField.UpdatedAt => "updated_at",
            _ => null,
        }, q.SortOrder, null, null);

    internal static string BuildQuery(LicenseListQuery? q) =>
        q is null ? "" : BuildQuery(q.Page, q.Limit, q.SearchQuery, q.SortField switch
        {
            LicenseSortField.CreatedAt => "created_at",
            LicenseSortField.Key => "key",
            LicenseSortField.ShortKey => "short_key",
            _ => null,
        }, q.SortOrder, q.ShortKey, q.Key);

    private static string BuildQuery(int? page, int? limit, string? search, string? sortField, SortOrder? order, string? shortKey, string? key)
    {
        var parts = new List<string>();
        void Add(string name, string? value)
        {
            if (!string.IsNullOrEmpty(value)) parts.Add(name + "=" + Uri.EscapeDataString(value!));
        }

        Add("page", page?.ToString(CultureInfo.InvariantCulture));
        Add("limit", limit?.ToString(CultureInfo.InvariantCulture));
        Add("search_query", search);
        Add("sort_field", sortField);
        Add("sort_order", order switch { SortOrder.Ascending => "asc", SortOrder.Descending => "desc", _ => null });
        Add("short_key", shortKey);
        Add("key", key);
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    public void Dispose()
    {
        if (_ownsHttpClient) _http.Dispose();
    }
}
