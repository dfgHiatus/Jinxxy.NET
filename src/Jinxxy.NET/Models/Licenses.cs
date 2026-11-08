using System;

namespace Jinxxy.Models;

/// <summary>License as returned by <c>GET /licenses</c>.</summary>
public record LicenseSummary
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";
    public JinxxyUser User { get; init; } = new();
    public string ShortKey { get; init; } = "";
}

/// <summary>Full license as returned by <c>GET /licenses/{id}</c>.</summary>
public sealed record License : LicenseSummary
{
    public string Key { get; init; } = "";
    public InventoryItem InventoryItem { get; init; } = new();
    public ActivationCount Activations { get; init; } = new();
}

public sealed record ActivationCount
{
    public int TotalCount { get; init; }
}

public sealed record InventoryItem
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";
    public string TargetId { get; init; } = "";
    public string? TargetVersionId { get; init; }
    public string TargetType { get; init; } = "";

    /// <summary>Order item ID if <see cref="GrantType"/> is ORDER_ITEM.</summary>
    public string? GrantId { get; init; }

    /// <summary>ORDER_ITEM, GIFT, LOYALTY_REWARD.</summary>
    public string? GrantType { get; init; }
    public PurchasedProduct Item { get; init; } = new();
    public InventoryOrder? Order { get; init; }
}

public sealed record PurchasedProduct
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";
    public string Name { get; init; } = "";
    public PurchasedVersion? Version { get; init; }
}

public sealed record PurchasedVersion
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";
    public string Name { get; init; } = "";
}

public sealed record InventoryOrder
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";

    /// <summary>PAID, REFUNDED, PARTIALLY_REFUNDED.</summary>
    public string PaymentStatus { get; init; } = "";
}

public sealed record LicenseActivation
{
    public string Id { get; init; } = "";
    public string Object { get; init; } = "";
    public string LicenseId { get; init; } = "";
    public string Description { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>Page of activations; unlike other pages it also reports a total count.</summary>
public sealed class LicenseActivationPage : PagedResult<LicenseActivation>
{
    public int TotalCount { get; init; }
}

public sealed record CreateLicenseActivationRequest
{
    /// <summary>Free-form description, e.g. <c>APP_NAME:FEATURE_FLAG</c>.</summary>
    public string Description { get; init; } = "";
}

public sealed record DeleteActivationResult
{
    public bool Deleted { get; init; }
}
