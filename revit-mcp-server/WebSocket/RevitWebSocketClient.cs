using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using RevitMcpServer.Models;

namespace RevitMcpServer.WebSocket;

/// <summary>
/// Manages the TCP connection to the Revit plugin.
/// Uses JSON-RPC 2.0 over raw TCP, matching the existing plugin protocol.
/// Connects lazily on first use and reconnects automatically on failure.
/// Requests are serialised (one at a time) using a semaphore.
///
/// Port is read from the REVIT_PLUGIN_PORT environment variable (default: 8080).
/// The Revit plugin's port must be updated to match in plugin/Core/SocketService.cs line 87.
/// </summary>
public sealed class RevitWebSocketClient : IDisposable
{
    private const string Host = "localhost";
    private static readonly int Port = int.TryParse(
        Environment.GetEnvironmentVariable("REVIT_PLUGIN_PORT"), out var p) ? p : 8080;
    private const int TimeoutMs = 120_000; // 2 minutes — matches TypeScript server

    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = null,          // preserve casing of anonymous types
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<string> SendCommandAsync(string command, object @params,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Ensure we have a live connection; reconnect if necessary.
            await EnsureConnectedAsync(cancellationToken);

            var requestId = Guid.NewGuid().ToString();
            var request = new RevitCommand
            {
                Method = command,
                Params = @params,
                Id = requestId
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(requestJson);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeoutMs);

            try
            {
                await _stream!.WriteAsync(bytes, cts.Token);
                var responseJson = await ReadCompleteJsonAsync(_stream, cts.Token);
                return ParseResponse(responseJson, requestId);
            }
            catch (Exception)
            {
                // Drop the connection so the next call reconnects.
                DisposeConnection();
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_tcp?.Connected == true && _stream != null)
            return;

        DisposeConnection();
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(Host, Port, ct);
        _stream = _tcp.GetStream();
    }

    /// <summary>
    /// Reads bytes from the stream until the accumulated buffer is valid JSON.
    /// The plugin writes the entire response in one shot, but TCP may fragment it.
    /// </summary>
    private static async Task<string> ReadCompleteJsonAsync(NetworkStream stream,
        CancellationToken ct)
    {
        var buffer = new byte[8192];
        var accumulated = new MemoryStream();

        while (true)
        {
            int read = await stream.ReadAsync(buffer, ct);
            if (read == 0)
                throw new IOException("Revit plugin closed the connection before sending a complete response.");

            accumulated.Write(buffer, 0, read);

            // Try to parse what we have so far.
            accumulated.Position = 0;
            try
            {
                using var doc = await JsonDocument.ParseAsync(accumulated, cancellationToken: ct);
                accumulated.Position = 0;
                return new StreamReader(accumulated, Encoding.UTF8).ReadToEnd();
            }
            catch (JsonException)
            {
                // Incomplete JSON — continue reading.
                accumulated.Position = accumulated.Length;
            }
        }
    }

    private static string ParseResponse(string responseJson, string expectedId)
    {
        var response = JsonSerializer.Deserialize<RevitResponse>(responseJson)
            ?? throw new InvalidOperationException("Received null response from Revit plugin.");

        if (response.Error is { } error)
            throw new InvalidOperationException(
                $"Revit plugin returned error {error.Code}: {error.Message}");

        return response.Result?.GetRawText() ?? "null";
    }

    private void DisposeConnection()
    {
        _stream?.Dispose();
        _tcp?.Dispose();
        _stream = null;
        _tcp = null;
    }

    public void Dispose()
    {
        _lock.Dispose();
        DisposeConnection();
    }
}
