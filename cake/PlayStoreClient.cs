using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Build;

/// <summary>
/// Schlanker Client fuer die Google Play Developer API (androidpublisher v3) auf Basis
/// des Service-Account-JSON-Keys (secrets/play-store-key.json). Ersetzt "fastlane supply",
/// damit die Android-Release-Pipeline ohne Ruby/fastlane auf Windows laeuft.
/// Ablauf: CreateEdit -> Aenderungen (Listings/Images/Bundles/Tracks/Details) -> CommitEdit.
/// Ein nie committeter Edit verfaellt serverseitig automatisch (fuer Read-only-Abfragen ok).
/// </summary>
public sealed class PlayStoreClient : IDisposable
{
    private const string BaseUrl = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications";
    private const string UploadBaseUrl = "https://androidpublisher.googleapis.com/upload/androidpublisher/v3/applications";

    private readonly HttpClient _http;
    private readonly string _packageName;

    public PlayStoreClient(string serviceAccountJsonPath, string packageName)
    {
        _packageName = packageName;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        var accessToken = GetAccessToken(serviceAccountJsonPath);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public string CreateEdit()
    {
        var response = Send(HttpMethod.Post, $"{BaseUrl}/{_packageName}/edits");
        using var doc = JsonDocument.Parse(response);
        return doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("edits.insert returned no id");
    }

    /// <summary>
    /// Committet den Edit. Faellt bei "changes cannot be sent for review automatically"
    /// (z. B. verwaltete Veroeffentlichung/offene Deklarationen) auf
    /// changesNotSentForReview=true zurueck - gleiche Strategie wie fastlane.
    /// </summary>
    public void CommitEdit(string editId)
    {
        try
        {
            Send(HttpMethod.Post, $"{BaseUrl}/{_packageName}/edits/{editId}:commit");
        }
        catch (PlayStoreApiException ex) when (ex.Body.Contains("changesNotSentForReview", StringComparison.OrdinalIgnoreCase))
        {
            Send(HttpMethod.Post, $"{BaseUrl}/{_packageName}/edits/{editId}:commit?changesNotSentForReview=true");
        }
    }

    /// <summary>Hoechster Version-Code ueber alle Tracks (Baseline fuer den Version-Bump).</summary>
    public int GetHighestVersionCode(string editId)
    {
        var response = Send(HttpMethod.Get, $"{BaseUrl}/{_packageName}/edits/{editId}/tracks");
        using var doc = JsonDocument.Parse(response);

        var highest = 0;
        if (!doc.RootElement.TryGetProperty("tracks", out var tracks))
            return highest;

        foreach (var track in tracks.EnumerateArray())
        {
            if (!track.TryGetProperty("releases", out var releases))
                continue;
            foreach (var release in releases.EnumerateArray())
            {
                if (!release.TryGetProperty("versionCodes", out var codes))
                    continue;
                foreach (var code in codes.EnumerateArray())
                {
                    // versionCodes sind int64-as-string im JSON
                    if (long.TryParse(code.GetString(), out var value) && value > highest)
                        highest = (int)value;
                }
            }
        }
        return highest;
    }

    public void UpdateListing(string editId, string language, string title, string shortDescription, string fullDescription, string? videoUrl)
    {
        var body = JsonSerializer.Serialize(new Dictionary<string, string?>
        {
            ["language"] = language,
            ["title"] = title,
            ["shortDescription"] = shortDescription,
            ["fullDescription"] = fullDescription,
            ["video"] = string.IsNullOrWhiteSpace(videoUrl) ? null : videoUrl
        });
        Send(HttpMethod.Put, $"{BaseUrl}/{_packageName}/edits/{editId}/listings/{language}", body, "application/json");
    }

    /// <summary>Kontaktdaten/Default-Sprache der App ("Store-Eintrag &gt; Kontaktdetails").</summary>
    public void UpdateAppDetails(string editId, string? contactEmail, string? contactPhone, string? contactWebsite, string? defaultLanguage)
    {
        var fields = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(contactEmail)) fields["contactEmail"] = contactEmail;
        if (!string.IsNullOrWhiteSpace(contactPhone)) fields["contactPhone"] = contactPhone;
        if (!string.IsNullOrWhiteSpace(contactWebsite)) fields["contactWebsite"] = contactWebsite;
        if (!string.IsNullOrWhiteSpace(defaultLanguage)) fields["defaultLanguage"] = defaultLanguage;
        if (fields.Count == 0)
            return;

        // PATCH aktualisiert nur die gesendeten Felder
        Send(HttpMethod.Patch, $"{BaseUrl}/{_packageName}/edits/{editId}/details", JsonSerializer.Serialize(fields), "application/json");
    }

    /// <summary>Loescht alle Bilder eines Typs (phoneScreenshots, icon, featureGraphic, ...).</summary>
    public void DeleteAllImages(string editId, string language, string imageType)
    {
        Send(HttpMethod.Delete, $"{BaseUrl}/{_packageName}/edits/{editId}/listings/{language}/{imageType}");
    }

    public void UploadImage(string editId, string language, string imageType, string filePath)
    {
        var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "image/png"
        };
        var url = $"{UploadBaseUrl}/{_packageName}/edits/{editId}/listings/{language}/{imageType}?uploadType=media";
        SendBytes(HttpMethod.Post, url, File.ReadAllBytes(filePath), contentType);
    }

    /// <summary>Laedt das AAB hoch und liefert den enthaltenen Version-Code zurueck.</summary>
    public int UploadBundle(string editId, string aabPath)
    {
        var url = $"{UploadBaseUrl}/{_packageName}/edits/{editId}/bundles?uploadType=media";
        var response = SendBytes(HttpMethod.Post, url, File.ReadAllBytes(aabPath), "application/octet-stream");
        using var doc = JsonDocument.Parse(response);
        return doc.RootElement.GetProperty("versionCode").GetInt32();
    }

    /// <summary>Setzt den Release auf einem Track inkl. Release-Notes pro Sprache.</summary>
    public void SetTrackRelease(string editId, string track, int versionCode, string status, string releaseName, IReadOnlyDictionary<string, string> releaseNotes)
    {
        var body = JsonSerializer.Serialize(new
        {
            track,
            releases = new[]
            {
                new
                {
                    name = releaseName,
                    versionCodes = new[] { versionCode.ToString() },
                    status,
                    releaseNotes = releaseNotes
                        .Select(kv => new { language = kv.Key, text = kv.Value })
                        .ToArray()
                }
            }
        });
        Send(HttpMethod.Put, $"{BaseUrl}/{_packageName}/edits/{editId}/tracks/{track}", body, "application/json");
    }

    private string Send(HttpMethod method, string url, string? body = null, string? contentType = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body != null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, contentType ?? "application/json");
        }
        return Execute(request);
    }

    private string SendBytes(HttpMethod method, string url, byte[] payload, string contentType)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Content = new ByteArrayContent(payload);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return Execute(request);
    }

    private string Execute(HttpRequestMessage request)
    {
        var response = _http.Send(request);
        using var reader = new StreamReader(response.Content.ReadAsStream());
        var responseBody = reader.ReadToEnd();
        if (!response.IsSuccessStatusCode)
        {
            throw new PlayStoreApiException(
                $"Play API {request.Method} {request.RequestUri} failed: {(int)response.StatusCode} {response.ReasonPhrase}",
                responseBody);
        }
        return responseBody;
    }

    /// <summary>Service-Account-JWT (RS256) gegen einen OAuth2-Access-Token tauschen.</summary>
    private static string GetAccessToken(string serviceAccountJsonPath)
    {
        if (!File.Exists(serviceAccountJsonPath))
            throw new FileNotFoundException($"Play Store service account key not found: {serviceAccountJsonPath}");

        using var keyDoc = JsonDocument.Parse(File.ReadAllText(serviceAccountJsonPath));
        var clientEmail = keyDoc.RootElement.GetProperty("client_email").GetString()!;
        var privateKeyPem = keyDoc.RootElement.GetProperty("private_key").GetString()!;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = Base64UrlEncode(Encoding.UTF8.GetBytes("""{"alg":"RS256","typ":"JWT"}"""));
        var claims = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            iss = clientEmail,
            scope = "https://www.googleapis.com/auth/androidpublisher",
            aud = "https://oauth2.googleapis.com/token",
            iat = now,
            exp = now + 3600
        })));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var signature = rsa.SignData(
            Encoding.UTF8.GetBytes($"{header}.{claims}"),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var jwt = $"{header}.{claims}.{Base64UrlEncode(signature)}";

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var tokenResponse = http.Send(new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = jwt
            })
        });
        using var tokenReader = new StreamReader(tokenResponse.Content.ReadAsStream());
        var tokenBody = tokenReader.ReadToEnd();
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Google OAuth token exchange failed: {(int)tokenResponse.StatusCode} - {tokenBody}");
        }

        using var tokenDoc = JsonDocument.Parse(tokenBody);
        return tokenDoc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("OAuth response contained no access_token");
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public void Dispose() => _http.Dispose();
}

public sealed class PlayStoreApiException(string message, string body) : Exception($"{message}\n{body}")
{
    public string Body { get; } = body;
}
