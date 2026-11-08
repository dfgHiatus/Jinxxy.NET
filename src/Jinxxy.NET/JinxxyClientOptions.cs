using System;

namespace Jinxxy;

public sealed class JinxxyClientOptions
{
    public static readonly Uri DefaultBaseAddress = new("https://api.creators.jinxxy.com/v1/");

    /// <summary>API root. Must end with a slash.</summary>
    public Uri BaseAddress { get; set; } = DefaultBaseAddress;

    /// <summary>
    /// When true (default), a <c>429 Too Many Requests</c> response is retried after waiting for the
    /// rate-limit window to reset, up to <see cref="MaxRateLimitRetries"/> times.
    /// </summary>
    public bool RetryOnRateLimit { get; set; } = true;

    public int MaxRateLimitRetries { get; set; } = 2;

    /// <summary>Upper bound on how long a single rate-limit wait may take.</summary>
    public TimeSpan MaxRateLimitWait { get; set; } = TimeSpan.FromSeconds(65);

    /// <summary>Sent as the User-Agent header.</summary>
    public string UserAgent { get; set; } =
        "Jinxxy.NET/" + (typeof(JinxxyClientOptions).Assembly.GetName().Version?.ToString(3) ?? "0.0.0");
}
