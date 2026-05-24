using System.Text.Json;
using System.Text.Json.Serialization;

namespace RevitMcpServer.Models;

/// <summary>JSON-RPC 2.0 response received from the Revit plugin.</summary>
public sealed class RevitResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("result")]
    public JsonElement? Result { get; init; }

    [JsonPropertyName("error")]
    public RevitError? Error { get; init; }
}

public sealed class RevitError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }
}
