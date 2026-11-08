using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jinxxy.Models;

public sealed record ProductVersionSummary
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
}

/// <summary>Product as returned by <c>GET /products</c>.</summary>
public sealed record ProductSummary
{
    public string Object { get; init; } = "";
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public IReadOnlyList<ProductVersionSummary> Versions { get; init; } = [];
}

public sealed record ProductVersion
{
    public string Object { get; init; } = "";
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
}

/// <summary>Full product as returned by <c>GET /products/{id}</c>.</summary>
public sealed record Product
{
    public string Object { get; init; } = "";
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public IReadOnlyList<ProductVersion> Versions { get; init; } = [];
    public decimal BasePrice { get; init; }
    public bool PwywPricing { get; init; }
    public string CurrencyCode { get; init; } = "";
    public IReadOnlyList<string>? Restrictions { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<string>? Type { get; init; }
    public string? Gender { get; init; }
    public IReadOnlyList<string>? Platforms { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string Visibility { get; init; } = "";
    public string Url { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Any fields the API returned that this version of the client doesn't model yet.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
