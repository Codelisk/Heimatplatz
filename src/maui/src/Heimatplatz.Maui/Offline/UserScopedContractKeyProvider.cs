using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Debug.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using Shiny.Mediator.Infrastructure;

namespace Heimatplatz.Maui.Offline;

/// <summary>
/// Erzeugt stabile, benutzerspezifische Schluessel fuer Shiny Offline und Cache.
/// Der Standardprovider bildet Collections nur ueber ToString() ab; bei den
/// umfangreichen Suchfiltern koennte das zu Kollisionen fuehren.
/// </summary>
internal sealed class UserScopedContractKeyProvider(
    IAuthService authService,
    IConfiguration configuration,
    ILogger<UserScopedContractKeyProvider> logger
) : IContractKeyProvider
{
    private static readonly JsonSerializerOptions KeyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public string GetContractKey(object contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var contractType = contract.GetType();
        string payload;
        try
        {
            payload = contract is IContractKey explicitKey
                ? explicitKey.GetKey()
                : SerializeContract(contract, contractType);
        }
        catch (Exception ex)
        {
            // Ein Schluessel muss auch bei einem kuenftig nicht serialisierbaren
            // Request gebildet werden koennen. Der Typ bleibt dabei eindeutig.
            logger.LogWarning(ex, "Fallback fuer Offline-Schluessel von {ContractType}", contractType.FullName);
            payload = contract.ToString() ?? contractType.Name;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var endpoint = configuration[ApiEndpoints.MediatorHttpConfigKey] ?? string.Empty;
        var endpointHash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(endpoint)))[..12];

        return $"{GetScopePrefix(authService.UserId)}endpoint:{endpointHash}|{contractType.FullName}|{hash}";
    }

    public static string GetScopePrefix(Guid? userId)
        => userId is { } id ? $"user:{id:N}|" : "anonymous|";

    private static string SerializeContract(object contract, Type contractType)
    {
        var node = JsonSerializer.SerializeToNode(contract, contractType, KeyJsonOptions);

        // Der Altersfilter wird als "UtcNow minus Zeitraum" an die API gesendet.
        // Der exakte Timestamp aendert sich bei jedem Aufruf, fachlich ist es aber
        // weiterhin derselbe Filter. Fuer Offline/Cache wird daraus ein stabiler
        // Tagesabstand (1, 7, 28-31, 365/366 Tage).
        if (contractType.Name == "GetPropertiesHttpRequest" && node is JsonObject obj)
        {
            var createdAfterProperty = obj.FirstOrDefault(
                pair => pair.Key.Equals("createdAfter", StringComparison.OrdinalIgnoreCase));

            if (createdAfterProperty.Value is JsonValue value &&
                TryGetDateTimeOffset(value, out var createdAfter))
            {
                var ageInDays = (int)Math.Round((DateTimeOffset.UtcNow - createdAfter).TotalDays);
                obj[createdAfterProperty.Key] = $"age-days:{ageInDays}";
            }
        }

        return node?.ToJsonString(KeyJsonOptions) ?? "null";
    }

    private static bool TryGetDateTimeOffset(JsonValue value, out DateTimeOffset result)
    {
        if (value.TryGetValue<DateTimeOffset>(out result))
            return true;

        return value.TryGetValue<string>(out var text) &&
               DateTimeOffset.TryParse(
                   text,
                   System.Globalization.CultureInfo.InvariantCulture,
                   System.Globalization.DateTimeStyles.RoundtripKind,
                   out result);
    }
}
