# Jinxxy.NET

An unofficial .NET client for the [Jinxxy Creator API (v1)](https://api.creators.jinxxy.com/v1/docs).

- Targets **netstandard2.0** (Unity, Godot, .NET Framework 4.6.1+, older .NET) and **net10.0**.
- Trimming- and Native AOT-safe on net10.0, using source-generated `System.Text.Json`. Works on iOS, Android and WebAssembly.
- Covers every v1 endpoint: user, products, orders, licenses and activations, customers, and discount codes.
- Typed errors, automatic rate-limit retries and paginated enumeration built in.

> Not affiliated with or endorsed by Jinxxy.

## Getting an API key

API keys are created in the Jinxxy creator dashboard. Each key is granted **scopes**, and every endpoint needs one:

| Scope | Allows |
|---|---|
| `products_read` | `ListProductsAsync`, `GetProductAsync` |
| `orders_read` | `ListOrdersAsync`, `GetOrderAsync` |
| `licenses_read` | `ListLicensesAsync`, `GetLicenseAsync`, `ListLicenseActivationsAsync`, `GetLicenseActivationAsync` |
| `licenses_write` | `CreateLicenseActivationAsync`, `DeleteLicenseActivationAsync` |
| `customers_read` | `ListCustomersAsync`, `GetCustomerAsync` |
| `discount_codes_read` | `ListDiscountCodesAsync`, `GetDiscountCodeAsync` |
| `discount_codes_write` | `CreateDiscountCodeAsync`, `UpdateDiscountCodeAsync`, `DeleteDiscountCodeAsync` |

`GetMeAsync` works with any valid key and returns the scopes it has (see `JinxxyScopes` for constants).

Treat API keys like passwords. Don't ship them inside a client app that you distribute to other people.

## Usage

```csharp
using Jinxxy;
using Jinxxy.Models;

using var jinxxy = new JinxxyClient(apiKey);

// Who owns this key, and what can it do?
CurrentUser me = await jinxxy.GetMeAsync();
Console.WriteLine($"{me.DisplayName}: {string.Join(", ", me.Scopes)}");

// Every product, across all pages
await foreach (ProductSummary p in jinxxy.EnumerateProductsAsync())
    Console.WriteLine(p.Name);

// One page, with search and sorting
PagedResult<OrderSummary> orders = await jinxxy.ListOrdersAsync(new ListQuery
{
    Limit = 50,
    SearchQuery = "fox",
    SortField = ListSortField.CreatedAt,
    SortOrder = SortOrder.Descending,
});
```

### Verifying a license key

A common use: check a buyer's license key inside your tool or Unity package.

```csharp
var page = await jinxxy.ListLicensesAsync(new LicenseListQuery { Key = licenseKeyFromUser });
if (page.Results.Count == 0)
{
    Console.WriteLine("Invalid license");
    return;
}

License license = await jinxxy.GetLicenseAsync(page.Results[0].Id);
Console.WriteLine($"{license.InventoryItem.Item.Name}, activations: {license.Activations.TotalCount}");

await jinxxy.CreateLicenseActivationAsync(license.Id, "MY_TOOL:machine-1234");
```

### Discount codes

```csharp
await jinxxy.CreateDiscountCodeAsync(new CreateDiscountCodeRequest
{
    Name = "Summer Sale",
    Code = "SUMMER",
    Type = DiscountType.Percentage,
    Amount = 20,                       // percent; for FixedAmount this is in cents
    Scope = DiscountScope.Store,
    ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
});

// Partial update: only non-null fields are sent. Use Clear* flags to explicitly null a field.
await jinxxy.UpdateDiscountCodeAsync(codeId, new UpdateDiscountCodeRequest { ClearExpiresAt = true });
```

## Errors

Any non-2xx response throws `JinxxyApiException`:

```csharp
try
{
    await jinxxy.GetProductAsync("does-not-exist");
}
catch (JinxxyApiException ex) when (ex.IsNotFound)
{
    Console.WriteLine($"{ex.ErrorCode}: {ex.Message} (request {ex.RequestId})");
}
```

It exposes `StatusCode`, the parsed `Error` body, `ErrorCode` (e.g. `INVALID_API_KEY`, `RATE_LIMIT_EXCEEDED`), `RequestId` (include it when contacting Jinxxy support), and the helpers `IsUnauthorized`, `IsForbidden`, `IsNotFound` and `IsRateLimited`.

## Rate limiting

The API allows **120 requests per minute** per key. By default, a `429` is retried up to twice after waiting for the window to reset (honouring `Retry-After` / `X-RateLimit-Reset`, capped at 65 s). `client.LastRateLimit` exposes the most recent rate-limit headers.

```csharp
var jinxxy = new JinxxyClient(apiKey, options: new JinxxyClientOptions
{
    RetryOnRateLimit = true,
    MaxRateLimitRetries = 3,
    MaxRateLimitWait = TimeSpan.FromSeconds(30),
    UserAgent = "MyTool/2.0",
});
```

## Bring your own HttpClient

Pass a shared `HttpClient` (e.g. from `IHttpClientFactory`) to control handlers, proxies and timeouts. The client won't dispose an `HttpClient` you pass in.

```csharp
var jinxxy = new JinxxyClient(apiKey, httpClient);
```

## Forward compatibility

`Product.AdditionalData` captures any fields the API returns that this library doesn't model yet, so new API fields are never silently lost.

## Building

```sh
dotnet build
dotnet test
dotnet pack src/Jinxxy.NET -c Release   # produces .nupkg + .snupkg
```
