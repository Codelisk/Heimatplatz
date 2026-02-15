using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Heimatplatz.Api.Features.Notifications.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.Notifications.Services;

/// <summary>
/// Custom APNs client using direct HTTP/2 - replaces Fitomad.Apns library
/// </summary>
public interface IApnsService
{
    Task<ApnsResult> SendAsync(string deviceToken, string title, string body,
        string? category = null, string? threadId = null, Dictionary<string, string>? customData = null,
        CancellationToken cancellationToken = default);
}

public record ApnsResult(bool IsSuccess, int StatusCode, string? ErrorReason = null);

public class ApnsService : IApnsService
{
    private readonly HttpClient _httpClient;
    private readonly ApnsOptions _apnsOptions;
    private readonly ILogger<ApnsService> _logger;
    private readonly ECDsa _key;

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry;

    public ApnsService(
        IHttpClientFactory httpClientFactory,
        IOptions<PushNotificationOptions> options,
        ILogger<ApnsService> logger)
    {
        _apnsOptions = options.Value.Apns;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ApnsClient");

        var host = _apnsOptions.UseProduction
            ? "https://api.push.apple.com"
            : "https://api.sandbox.push.apple.com";
        _httpClient.BaseAddress = new Uri(host);
        _httpClient.DefaultRequestVersion = new Version(2, 0);
        _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

        // Load the private key
        var keyContent = _apnsOptions.PrivateKeyContent ?? "";
        keyContent = keyContent
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();

        _key = ECDsa.Create();
        _key.ImportPkcs8PrivateKey(Convert.FromBase64String(keyContent), out _);
    }

    public async Task<ApnsResult> SendAsync(string deviceToken, string title, string body,
        string? category = null, string? threadId = null, Dictionary<string, string>? customData = null,
        CancellationToken cancellationToken = default)
    {
        var token = GetOrRefreshJwt();

        var payload = new ApnsPayload
        {
            Aps = new ApsPayload
            {
                Alert = new ApsAlert { Title = title, Body = body },
                Sound = "default",
                Category = category
            }
        };
        if (threadId != null) payload.Aps.ThreadId = threadId;

        var json = JsonSerializer.Serialize(payload, ApnsJsonContext.Default.ApnsPayload);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/3/device/{deviceToken.ToLowerInvariant()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Headers.TryAddWithoutValidation("apns-topic", _apnsOptions.BundleId);
        request.Headers.TryAddWithoutValidation("apns-push-type", "alert");
        request.Headers.TryAddWithoutValidation("apns-priority", "10");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                return new ApnsResult(true, statusCode);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            string? reason = null;
            try
            {
                var errorDoc = JsonDocument.Parse(responseBody);
                reason = errorDoc.RootElement.GetProperty("reason").GetString();
            }
            catch { reason = responseBody; }

            _logger.LogWarning("APNs error {StatusCode}: {Reason}", statusCode, reason);
            return new ApnsResult(false, statusCode, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "APNs request failed for device {Token}", deviceToken[..Math.Min(20, deviceToken.Length)]);
            return new ApnsResult(false, 0, ex.Message);
        }
    }

    private string GetOrRefreshJwt()
    {
        if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiry)
            return _cachedToken;

        var now = DateTimeOffset.UtcNow;

        // Build JWT header and payload manually (APNs wants only alg+kid, no typ)
        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
            new { alg = "ES256", kid = _apnsOptions.KeyId }));
        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
            new { iss = _apnsOptions.TeamId, iat = now.ToUnixTimeSeconds() }));

        var signingInput = Encoding.ASCII.GetBytes($"{header}.{payload}");
        var signature = _key.SignData(signingInput, HashAlgorithmName.SHA256);

        // Convert DER signature to fixed-length r||s format for ES256
        // ECDsa.SignData with DSASignatureFormat not available on all platforms,
        // so we use the default IEEE P1363 format which is already r||s
        var signatureB64 = Base64UrlEncode(signature);

        _cachedToken = $"{header}.{payload}.{signatureB64}";
        _tokenExpiry = now.AddMinutes(50); // Apple allows 60 min, refresh at 50

        return _cachedToken;
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

// JSON models for APNs payload
internal class ApnsPayload
{
    [JsonPropertyName("aps")]
    public ApsPayload Aps { get; set; } = new();
}

internal class ApsPayload
{
    [JsonPropertyName("alert")]
    public ApsAlert Alert { get; set; } = new();

    [JsonPropertyName("sound")]
    public string? Sound { get; set; }

    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Category { get; set; }

    [JsonPropertyName("thread-id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ThreadId { get; set; }
}

internal class ApsAlert
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";
}

[JsonSerializable(typeof(ApnsPayload))]
internal partial class ApnsJsonContext : JsonSerializerContext;
