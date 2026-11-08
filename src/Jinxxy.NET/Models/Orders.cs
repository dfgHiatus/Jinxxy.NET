using System;
using System.Collections.Generic;

namespace Jinxxy.Models;

/// <summary>Order as returned by <c>GET /orders</c>.</summary>
public record OrderSummary
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";
    public string Email { get; init; } = "";
    public DateTimeOffset PaidAt { get; init; }
    public JinxxyUser User { get; init; } = new();
    public string PaymentStatus { get; init; } = "";
    public decimal PayoutTotal { get; init; }
}

/// <summary>Full order as returned by <c>GET /orders/{id}</c>.</summary>
public sealed record Order : OrderSummary
{
    public IReadOnlyList<CheckoutAnswer> CheckoutFields { get; init; } = [];
    public IReadOnlyList<OrderItem> OrderItems { get; init; } = [];
}

public sealed record CheckoutAnswer
{
    public string Object { get; init; } = "";
    public string Answer { get; init; } = "";
    public string Label { get; init; } = "";
}

public sealed record OrderItem
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";
    public string Name { get; init; } = "";
    public string? TargetId { get; init; }

    /// <summary>DIGITAL_PRODUCT, PLATFORM_FEE, DISCOUNT, ...</summary>
    public string TargetType { get; init; } = "";
    public string? TargetVersionId { get; init; }
    public JinxxyUser? Seller { get; init; }
    public string? LicenseId { get; init; }
    public LicenseKey? License { get; init; }
}

/// <summary>License key details embedded in an <see cref="OrderItem"/>.</summary>
public sealed record LicenseKey
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";
    public string Key { get; init; } = "";
    public string ShortKey { get; init; } = "";
}
