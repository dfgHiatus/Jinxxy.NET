using System.Net;
using System.Text;
using System.Text.Json;
using Jinxxy;
using Jinxxy.Models;
using Xunit;

namespace Jinxxy.Tests;

public class JinxxyClientTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            Requests.Add((request, body));
            return respond(request);
        }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var r = new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        r.Headers.Add("X-RateLimit-Limit", "120");
        r.Headers.Add("X-RateLimit-Remaining", "119");
        r.Headers.Add("X-RateLimit-Reset", "1704067260");
        r.Headers.Add("X-Request-ID", "req-123");
        return r;
    }

    private static (JinxxyClient, FakeHandler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new FakeHandler(respond);
        return (new JinxxyClient("  secret-key ", new HttpClient(handler)), handler);
    }

    [Fact]
    public async Task GetMe_SendsApiKeyAndParsesSnakeCase()
    {
        var (client, handler) = Create(_ => Json("""
            { "id": "u1", "object": "User", "name": "Neko", "username": "neko",
              "profile_image": { "id": "p", "object": "ProfileImageFile", "url": "https://cdn/x.png", "updated_at": "2025-01-01T00:00:00Z" },
              "updated_at": "2025-02-01T00:00:00Z", "scopes": ["products_read", "orders_read"] }
            """));

        var me = await client.GetMeAsync();

        var req = handler.Requests.Single().Request;
        Assert.Equal("https://api.creators.jinxxy.com/v1/me", req.RequestUri!.ToString());
        Assert.Equal("secret-key", req.Headers.GetValues("x-api-key").Single());
        Assert.Equal("Neko", me.DisplayName);
        Assert.Equal("https://cdn/x.png", me.ProfileImage!.Url);
        Assert.Contains(JinxxyScopes.ProductsRead, me.Scopes);
        Assert.Equal(new RateLimitInfo(120, 119, DateTimeOffset.FromUnixTimeSeconds(1704067260)), client.LastRateLimit);
    }

    [Fact]
    public async Task ListProducts_BuildsQueryString()
    {
        var (client, handler) = Create(_ => Json("""{ "page": 1, "cursor_count": 0, "page_count": 1, "results": [] }"""));

        await client.ListProductsAsync(new ListQuery
        {
            Page = 2, Limit = 50, SearchQuery = "cat ears & tail",
            SortField = ListSortField.CreatedAt, SortOrder = SortOrder.Descending,
        });

        Assert.Equal(
            "https://api.creators.jinxxy.com/v1/products?page=2&limit=50&search_query=cat%20ears%20%26%20tail&sort_field=created_at&sort_order=desc",
            handler.Requests.Single().Request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateProducts_WalksAllPages()
    {
        var (client, handler) = Create(req =>
        {
            var page = req.RequestUri!.Query.Contains("page=2") ? 2 : 1;
            return Json($$"""
                { "page": {{page}}, "cursor_count": 3, "page_count": 2,
                  "results": [ { "object": "InternalProduct", "id": "p{{page}}", "name": "Product {{page}}", "versions": [] } ] }
                """);
        });

        var names = new List<string>();
        await foreach (var p in client.EnumerateProductsAsync(new ListQuery { Limit = 1 }))
            names.Add(p.Name);

        Assert.Equal(["Product 1", "Product 2"], names);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetProduct_ParsesFullDetailsAndKeepsUnknownFields()
    {
        var (client, _) = Create(_ => Json("""
            { "object": "InternalProduct", "id": "p1", "name": "Fox Avatar",
              "versions": [ { "object": "ProductVersion", "id": "v1", "name": "Standard", "price": 2500 } ],
              "base_price": 2500, "pwyw_pricing": false, "currency_code": "USD",
              "restrictions": null, "category": "avatar", "type": ["3d"], "gender": null,
              "platforms": ["vrchat"], "tags": ["fox", "quest"], "visibility": "PUBLIC",
              "url": "https://jinxxy.com/neko/fox", "created_at": "2024-05-01T10:00:00Z", "updated_at": "2024-06-01T10:00:00Z",
              "cover_image": "https://cdn/cover.png" }
            """));

        var product = await client.GetProductAsync("p1");

        Assert.Equal(2500m, product.Versions.Single().Price);
        Assert.Equal(["vrchat"], product.Platforms);
        Assert.Null(product.Gender);
        Assert.Equal(new DateTimeOffset(2024, 5, 1, 10, 0, 0, TimeSpan.Zero), product.CreatedAt);
        Assert.Equal("https://cdn/cover.png", product.AdditionalData!["cover_image"].GetString());
    }

    [Fact]
    public async Task ErrorResponse_ThrowsTypedException()
    {
        var (client, _) = Create(_ => Json("""
            { "status_code": 401, "error": "Unauthorized", "message": "Invalid API key", "code": "UNAUTHORIZED", "request_id": "abc" }
            """, HttpStatusCode.Unauthorized));

        var ex = await Assert.ThrowsAsync<JinxxyApiException>(() => client.GetMeAsync());

        Assert.True(ex.IsUnauthorized);
        Assert.Equal("UNAUTHORIZED", ex.ErrorCode);
        Assert.Equal("abc", ex.RequestId);
        Assert.Contains("Invalid API key", ex.Message);
    }

    [Fact]
    public async Task NonJsonError_StillThrowsWithRequestId()
    {
        var (client, _) = Create(_ =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>bad gateway</html>") };
            r.Headers.Add("X-Request-ID", "gw-1");
            return r;
        });

        var ex = await Assert.ThrowsAsync<JinxxyApiException>(() => client.GetMeAsync());

        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Null(ex.Error);
        Assert.Equal("gw-1", ex.RequestId);
    }

    [Fact]
    public async Task RateLimited_RetriesThenSucceeds()
    {
        var calls = 0;
        var (client, _) = Create(_ =>
        {
            if (calls++ == 0)
            {
                var r = Json("""{ "status_code": 429, "error": "Too Many Requests", "message": "slow down", "code": "RATE_LIMIT_EXCEEDED" }""",
                    (HttpStatusCode)429);
                r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
                return r;
            }
            return Json("""{ "id": "u1", "object": "User", "name": "", "username": "neko", "profile_image": null, "updated_at": "2025-01-01T00:00:00Z", "scopes": [] }""");
        });

        var me = await client.GetMeAsync();

        Assert.Equal(2, calls);
        Assert.Equal("neko", me.DisplayName); // falls back to username when name is empty
    }

    [Fact]
    public async Task RateLimited_ThrowsWhenRetryDisabled()
    {
        var handler = new FakeHandler(_ => Json("""{ "status_code": 429, "error": "Too Many Requests", "message": "x", "code": "RATE_LIMIT_EXCEEDED" }""", (HttpStatusCode)429));
        var client = new JinxxyClient("k", new HttpClient(handler), new JinxxyClientOptions { RetryOnRateLimit = false });

        var ex = await Assert.ThrowsAsync<JinxxyApiException>(() => client.GetMeAsync());

        Assert.True(ex.IsRateLimited);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task CreateDiscountCode_SerializesEnumsAndOmitsNulls()
    {
        var (client, handler) = Create(_ => Json("""
            { "object": "DiscountCode", "id": "d1", "name": "Summer", "code": "SUMMER", "use_count": 0, "use_limit": null,
              "expires_at": null, "scope": "STORE", "product_ids": [] }
            """, HttpStatusCode.Created));

        await client.CreateDiscountCodeAsync(new CreateDiscountCodeRequest
        {
            Name = "Summer", Code = "SUMMER", Type = DiscountType.FixedAmount, Amount = 1000, Scope = DiscountScope.Store,
        });

        var (req, body) = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, req.Method);
        using var doc = JsonDocument.Parse(body!);
        Assert.Equal("FIXED_AMOUNT", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("STORE", doc.RootElement.GetProperty("scope").GetString());
        Assert.False(doc.RootElement.TryGetProperty("use_limit", out _));
    }

    [Fact]
    public async Task UpdateDiscountCode_SendsExplicitNullOnlyWhenClearing()
    {
        var (client, handler) = Create(_ => Json("""
            { "object": "DiscountCode", "id": "d1", "name": "Summer", "code": "SUMMER", "use_count": 0, "use_limit": null,
              "expires_at": null, "scope": "STORE", "product_ids": [] }
            """));

        await client.UpdateDiscountCodeAsync("d1", new UpdateDiscountCodeRequest { Name = "Winter", ClearUseLimit = true });

        var (req, body) = handler.Requests.Single();
        Assert.Equal("PATCH", req.Method.Method);
        using var doc = JsonDocument.Parse(body!);
        Assert.Equal("Winter", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("use_limit").ValueKind);
        Assert.False(doc.RootElement.TryGetProperty("expires_at", out _));
        Assert.False(doc.RootElement.TryGetProperty("code", out _));
    }

    [Fact]
    public async Task Ids_AreEscapedInPaths()
    {
        var (client, handler) = Create(_ => Json("""{ "deleted": true }"""));

        var result = await client.DeleteLicenseActivationAsync("lic/1", "act 2");

        Assert.True(result.Deleted);
        Assert.Equal("/v1/licenses/lic%2F1/activations/act%202", handler.Requests.Single().Request.RequestUri!.AbsolutePath);
    }
}
