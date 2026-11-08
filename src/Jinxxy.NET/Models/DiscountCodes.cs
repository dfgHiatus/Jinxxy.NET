using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jinxxy.Models;

[JsonConverter(typeof(JsonStringEnumConverter<DiscountType>))]
public enum DiscountType
{
    [JsonStringEnumMemberName("FIXED_AMOUNT")] FixedAmount,
    [JsonStringEnumMemberName("PERCENTAGE")] Percentage,
}

[JsonConverter(typeof(JsonStringEnumConverter<DiscountScope>))]
public enum DiscountScope
{
    [JsonStringEnumMemberName("STORE")] Store,
    [JsonStringEnumMemberName("PRODUCT")] Product,
}

public sealed record DiscountCode
{
    public string Object { get; init; } = "";
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";

    /// <summary>FIXED_AMOUNT or PERCENTAGE (kept as a string so unknown values never break parsing).</summary>
    public string? Type { get; init; }
    public decimal? Amount { get; init; }
    public int UseCount { get; init; }
    public int? UseLimit { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string Scope { get; init; } = "";
    public IReadOnlyList<string> ProductIds { get; init; } = [];
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed record CreateDiscountCodeRequest
{
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public DiscountType Type { get; init; }

    /// <summary>Cents for <see cref="DiscountType.FixedAmount"/>, percent for <see cref="DiscountType.Percentage"/>.</summary>
    public int Amount { get; init; }
    public DiscountScope Scope { get; init; }

    /// <summary>Required when <see cref="Scope"/> is <see cref="DiscountScope.Product"/>.</summary>
    public IReadOnlyList<string>? ProductIds { get; init; }

    /// <summary>Maximum number of uses; null for unlimited.</summary>
    public int? UseLimit { get; init; }

    /// <summary>Expiration date; null for never.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// Partial update. Only non-null properties are sent. Because null means "leave unchanged",
/// use <see cref="ClearUseLimit"/> / <see cref="ClearExpiresAt"/> to explicitly set those fields to null.
/// </summary>
public sealed record UpdateDiscountCodeRequest
{
    public string? Name { get; init; }
    public string? Code { get; init; }
    public DiscountType? Type { get; init; }
    public int? Amount { get; init; }
    public DiscountScope? Scope { get; init; }
    public IReadOnlyList<string>? ProductIds { get; init; }
    public int? UseLimit { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Send <c>use_limit: null</c> (unlimited uses).</summary>
    [JsonIgnore] public bool ClearUseLimit { get; init; }

    /// <summary>Send <c>expires_at: null</c> (never expires).</summary>
    [JsonIgnore] public bool ClearExpiresAt { get; init; }
}

public sealed record DeleteDiscountCodeResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
}
