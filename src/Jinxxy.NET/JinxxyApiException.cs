using System;
using System.Net;
using Jinxxy.Models;

namespace Jinxxy;

/// <summary>Thrown when the Jinxxy API returns a non-success status code.</summary>
public sealed class JinxxyApiException : Exception
{
    public JinxxyApiException(HttpStatusCode statusCode, ApiError? error, string? requestId, RateLimitInfo? rateLimit)
        : base(BuildMessage(statusCode, error))
    {
        StatusCode = statusCode;
        Error = error;
        RequestId = error?.RequestId ?? requestId;
        RateLimit = rateLimit;
    }

    public HttpStatusCode StatusCode { get; }

    /// <summary>The parsed error body, when the server sent one.</summary>
    public ApiError? Error { get; }

    /// <summary>Machine-readable error code, e.g. <c>NOT_FOUND</c> or <c>RATE_LIMIT_EXCEEDED</c>.</summary>
    public string? ErrorCode => Error?.Code;

    /// <summary>Value of <c>X-Request-ID</c>; include it when contacting Jinxxy support.</summary>
    public string? RequestId { get; }
    public RateLimitInfo? RateLimit { get; }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    public bool IsRateLimited => (int)StatusCode == 429;

    private static string BuildMessage(HttpStatusCode status, ApiError? error)
    {
        var detail = error?.Message ?? error?.Error;
        return string.IsNullOrWhiteSpace(detail)
            ? $"Jinxxy API returned {(int)status} ({status})."
            : $"Jinxxy API returned {(int)status}: {detail}";
    }
}
