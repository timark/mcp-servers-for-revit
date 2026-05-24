using System.Text.Json.Serialization;

namespace RevitMcpServer.Models;

/// <summary>JSON-RPC 2.0 request sent to the Revit plugin over TCP.</summary>
public sealed class RevitCommand
{
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; init; } = "2.0";

    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("params")]
    public required object Params { get; init; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }
}
