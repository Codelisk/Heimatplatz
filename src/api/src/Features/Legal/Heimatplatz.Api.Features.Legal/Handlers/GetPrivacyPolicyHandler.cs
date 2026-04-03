using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Heimatplatz.Api.Features.Legal.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Handlers;

/// <summary>
/// Handler fuer GetPrivacyPolicyRequest - gibt die aktive Datenschutzerklaerung zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/legal")]
public class GetPrivacyPolicyHandler(AppDbContext dbContext) : IRequestHandler<GetPrivacyPolicyRequest, GetPrivacyPolicyResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [MediatorHttpGet("/privacy-policy", OperationId = "GetPrivacyPolicy")]
    public async Task<GetPrivacyPolicyResponse> Handle(GetPrivacyPolicyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var settings = await dbContext.Set<LegalSettings>()
            .Where(x => x.SettingType == "PrivacyPolicy" && x.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        // Fallback: Seed on-demand wenn keine Daten vorhanden
        if (settings == null)
        {
            await SeedPrivacyPolicyAsync(cancellationToken);
            settings = await dbContext.Set<LegalSettings>()
                .Where(x => x.SettingType == "PrivacyPolicy" && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (settings == null)
        {
            return new GetPrivacyPolicyResponse(null);
        }

        var responsibleParty = settings.ResponsiblePartyJson != null
            ? JsonSerializer.Deserialize<ResponsiblePartyDto>(settings.ResponsiblePartyJson, JsonOptions)
            : null;

        var sections = settings.SectionsJson != null
            ? JsonSerializer.Deserialize<List<LegalSectionDto>>(settings.SectionsJson, JsonOptions)
            : new List<LegalSectionDto>();

        if (responsibleParty == null)
        {
            return new GetPrivacyPolicyResponse(null);
        }

        var privacyPolicy = new PrivacyPolicyDto(
            responsibleParty,
            sections ?? new List<LegalSectionDto>(),
            settings.Version,
            settings.EffectiveDate,
            settings.UpdatedAt ?? settings.CreatedAt
        );

        return new GetPrivacyPolicyResponse(privacyPolicy);
    }

    private async Task SeedPrivacyPolicyAsync(CancellationToken cancellationToken)
    {
        var responsibleParty = new ResponsiblePartyDto(
            CompanyName: "Ing. Daniel Hufnagl",
            Street: "Stockham 44/Tuer 2",
            PostalCode: "4663",
            City: "Laakirchen",
            Country: "Oesterreich",
            Email: "info@heimatplatz.at",
            Phone: null,
            DataProtectionOfficer: null);

        var sections = new List<LegalSectionDto>
        {
            new(1, "Verantwortlicher", "Verantwortlicher im Sinne der Datenschutz-Grundverordnung (DSGVO) ist die im Abschnitt genannte Stelle."),
            new(2, "Welche Daten wir erheben", "Bei der Nutzung unserer Website/App werden folgende Daten verarbeitet:\n\n- Server-Logdaten (IP-Adresse, Zugriffszeitpunkt, Browser-Typ)\n- Registrierungsdaten (Name, E-Mail-Adresse, Passwort)\n- Nutzungsdaten (Sucheinstellungen, Favoriten, Kontaktanfragen)\n- Immobiliendaten bei Inseratserstellung"),
            new(3, "Zweck und Rechtsgrundlage", "Wir verarbeiten Ihre Daten zu folgenden Zwecken:\n\na) Vertragserfuellung (Art. 6 Abs. 1 lit. b DSGVO): Bereitstellung der Plattform, Verwaltung Ihres Benutzerkontos, Vermittlung von Immobilienanfragen.\n\nb) Berechtigte Interessen (Art. 6 Abs. 1 lit. f DSGVO): Gewaehrleistung der IT-Sicherheit, Analyse zur Verbesserung unserer Dienste, Betrugspraevention.\n\nc) Einwilligung (Art. 6 Abs. 1 lit. a DSGVO): Versand von Benachrichtigungen ueber neue Immobilien (sofern aktiviert)."),
            new(4, "Speicherdauer", "- Server-Logs: 30 Tage\n- Benutzerkonto-Daten: Bis zur Loeschung des Kontos\n- Kontaktanfragen: 3 Jahre nach Abschluss\n- Inserate: Bis zur Loeschung durch den Nutzer"),
            new(5, "Empfaenger der Daten", "Ihre Daten werden an folgende Empfaenger weitergegeben:\n\n- Hosting-Anbieter (Serverstandort: EU)\n- Immobilienanbieter bei Kontaktanfragen (nur freigegebene Daten)\n\nEine Uebermittlung in Drittlaender findet nicht statt."),
            new(6, "Ihre Rechte", "Sie haben folgende Rechte bezueglich Ihrer personenbezogenen Daten:\n\n- Auskunft ueber die gespeicherten Daten (Art. 15 DSGVO)\n- Berichtigung unrichtiger Daten (Art. 16 DSGVO)\n- Loeschung Ihrer Daten (Art. 17 DSGVO)\n- Einschraenkung der Verarbeitung (Art. 18 DSGVO)\n- Datenuebertragbarkeit (Art. 20 DSGVO)\n- Widerspruch gegen die Verarbeitung (Art. 21 DSGVO)\n- Widerruf einer erteilten Einwilligung (Art. 7 Abs. 3 DSGVO)"),
            new(7, "Beschwerderecht", "Sie haben das Recht, sich bei der zustaendigen Aufsichtsbehoerde zu beschweren:\n\nOesterreichische Datenschutzbehoerde\nBarichgasse 40-42\n1030 Wien\nE-Mail: dsb@dsb.gv.at\nWebsite: https://www.dsb.gv.at"),
            new(8, "Cookies und Local Storage", "Unsere Website verwendet ausschliesslich technisch notwendige Cookies bzw. Local Storage fuer:\n\n- Speicherung Ihrer Anmeldedaten (Session)\n- Speicherung Ihrer Filtereinstellungen\n\nFuer technisch notwendige Cookies ist keine Einwilligung erforderlich (Paragraph 165 Abs. 3 TKG)."),
            new(9, "Kontakt", "Bei Fragen zum Datenschutz wenden Sie sich bitte an die oben genannte E-Mail-Adresse.")
        };

        dbContext.Set<LegalSettings>().Add(new LegalSettings
        {
            SettingType = "PrivacyPolicy",
            ResponsiblePartyJson = JsonSerializer.Serialize(responsibleParty, JsonOptions),
            SectionsJson = JsonSerializer.Serialize(sections, JsonOptions),
            Version = "1.0",
            EffectiveDate = DateTimeOffset.UtcNow,
            IsActive = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
