using System;
using System.Collections.Generic;

namespace Jinxxy.Models;

/// <summary>A page of results returned by any list endpoint.</summary>
public class PagedResult<T>
{
    /// <summary>The 1-based page number of this page.</summary>
    public int Page { get; init; }

    /// <summary>Number of results reported by the cursor (as returned by the API).</summary>
    public int CursorCount { get; init; }

    /// <summary>Total number of pages available.</summary>
    public int PageCount { get; init; }

    public IReadOnlyList<T> Results { get; init; } = [];

    /// <summary>True when there are more pages after this one.</summary>
    public bool HasMore => Page < PageCount;
}

/// <summary>Profile picture attached to a <see cref="JinxxyUser"/>.</summary>
public sealed record ProfileImage
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";
    public string Url { get; init; } = "";
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>A public Jinxxy user (creator, customer or seller).</summary>
public record JinxxyUser
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";
    public string Name { get; init; } = "";
    public string Username { get; init; } = "";
    public ProfileImage? ProfileImage { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>The best name to show in a UI: display name, falling back to the username.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Username : Name;
}

/// <summary>The owner of the API key, as returned by <c>GET /me</c>.</summary>
public sealed record CurrentUser : JinxxyUser
{
    /// <summary>Scopes granted to the API key (see <see cref="JinxxyScopes"/>).</summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];
}

/// <summary>Well-known API key scopes.</summary>
public static class JinxxyScopes
{
    public const string ProductsRead = "products_read";
    public const string OrdersRead = "orders_read";
    public const string LicensesRead = "licenses_read";
    public const string LicensesWrite = "licenses_write";
    public const string CustomersRead = "customers_read";
    public const string DiscountCodesRead = "discount_codes_read";
    public const string DiscountCodesWrite = "discount_codes_write";
}

/// <summary>Error body returned by the API for any non-2xx response.</summary>
public sealed record ApiError
{
    public int StatusCode { get; init; }
    public string? Error { get; init; }
    public string? Message { get; init; }
    public string? Code { get; init; }
    public string? RequestId { get; init; }
}

/// <summary>Rate limit state reported by the most recent response.</summary>
public sealed record RateLimitInfo(int? Limit, int? Remaining, DateTimeOffset? ResetsAt);
