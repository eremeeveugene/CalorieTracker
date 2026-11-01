using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.Catalog.Infrastructure.Sources;

internal static class SourceJson
{
    // Food databases sometimes send numbers as strings ("serving_quantity": "15")
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static bool IsUnavailable(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or JsonException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);
}

internal static partial class SourceLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "{Source} is unavailable; continuing without it")]
    public static partial void Unavailable(ILogger logger, string source, Exception exception);
}
