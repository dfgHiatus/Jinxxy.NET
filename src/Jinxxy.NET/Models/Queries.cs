namespace Jinxxy.Models;

public enum SortOrder { Ascending, Descending }

public enum ListSortField { CreatedAt, UpdatedAt }

public enum LicenseSortField { CreatedAt, Key, ShortKey }

/// <summary>Common paging / searching parameters for list endpoints.</summary>
public sealed record ListQuery
{
    /// <summary>1-based page number.</summary>
    public int? Page { get; init; }

    /// <summary>Results per page.</summary>
    public int? Limit { get; init; }
    public string? SearchQuery { get; init; }
    public ListSortField? SortField { get; init; }
    public SortOrder? SortOrder { get; init; }
}

/// <summary>Parameters for <c>GET /licenses</c>, which supports extra sort fields and key filters.</summary>
public sealed record LicenseListQuery
{
    public int? Page { get; init; }
    public int? Limit { get; init; }
    public string? SearchQuery { get; init; }
    public LicenseSortField? SortField { get; init; }
    public SortOrder? SortOrder { get; init; }

    /// <summary>Filter by short key, e.g. <c>XXXX-cd071c534191</c>.</summary>
    public string? ShortKey { get; init; }

    /// <summary>Filter by full license key (UUID).</summary>
    public string? Key { get; init; }
}
