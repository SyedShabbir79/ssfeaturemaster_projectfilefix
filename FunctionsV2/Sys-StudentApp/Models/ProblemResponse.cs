using System.Text.Json.Serialization;

namespace FunctionsV2.Sys_StudentApp.Models;

public sealed class ProblemResponse
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "about:blank";

    [JsonPropertyName("title")]
    public string Title { get; init; } = "Error";

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("detail")]
    public string Detail { get; init; } = string.Empty;

    [JsonPropertyName("traceId")]
    public string TraceId { get; init; } = string.Empty;
}
