using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Marketing.Data.Seeding;

/// <summary>
/// Demo-Kontakte fuer Entwicklung/Test-System (IsDemoData=true, laeuft nie in Produktion).
/// Nur Kontakte, keine Mails/Rueckmeldungen - die entstehen durch echte Nutzung.
/// </summary>
public class MarketingSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 20;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<MarketingContact>().AnyAsync(cancellationToken))
            return;

        dbContext.Set<MarketingContact>().AddRange(
            new MarketingContact
            {
                Email = "maier.immobilien@example.at",
                Name = "Sabine Maier",
                Company = "Maier Immobilien GmbH",
                Phone = "+43 664 1111111",
                ContactType = MarketingContactType.Broker,
                Status = MarketingContactStatus.Lead,
                Source = "Manuell",
                Notes = "Maklerin aus Gmunden, auf ImmoScout aktiv."
            },
            new MarketingContact
            {
                Email = "office@hausverwaltung-huber.example.at",
                Name = "Franz Huber",
                Company = "Hausverwaltung Huber",
                ContactType = MarketingContactType.PropertyManager,
                Status = MarketingContactStatus.Lead,
                Source = "Manuell",
                Notes = "Verwaltet mehrere Objekte in Vöcklabruck."
            },
            new MarketingContact
            {
                Email = "gemeinde@laakirchen.example.at",
                Name = "Marktgemeinde Laakirchen",
                ContactType = MarketingContactType.Municipality,
                Status = MarketingContactStatus.Contacted,
                Source = "Manuell",
                LastContactedAt = DateTimeOffset.UtcNow.AddDays(-6),
                Notes = "Baugrund-Ausschreibungen; Ansprechpartner im Bauamt."
            },
            new MarketingContact
            {
                Email = "p.gruber@example.at",
                Name = "Peter Gruber",
                ContactType = MarketingContactType.PrivateSeller,
                Status = MarketingContactStatus.Replied,
                Source = "Versand",
                LastContactedAt = DateTimeOffset.UtcNow.AddDays(-10),
                LastReplyAt = DateTimeOffset.UtcNow.AddDays(-8),
                Notes = "Will sein Elternhaus in Ohlsdorf verkaufen, meldet sich im Herbst."
            },
            new MarketingContact
            {
                Email = "immo.steiner@example.at",
                Name = "Karin Steiner",
                Company = "Steiner Immobilien",
                ContactType = MarketingContactType.Broker,
                Status = MarketingContactStatus.Interested,
                Source = "Versand",
                LastContactedAt = DateTimeOffset.UtcNow.AddDays(-15),
                LastReplyAt = DateTimeOffset.UtcNow.AddDays(-13),
                Notes = "Möchte Details zum kostenlosen Inserieren, Telefontermin vereinbaren."
            },
            new MarketingContact
            {
                Email = "kontakt@bauprojekte-ooe.example.at",
                Name = "Bauprojekte OÖ",
                Company = "Bauprojekte OÖ GmbH",
                ContactType = MarketingContactType.Partner,
                Status = MarketingContactStatus.Customer,
                Source = "Manuell",
                LastContactedAt = DateTimeOffset.UtcNow.AddDays(-30),
                Notes = "Inseriert bereits regelmäßig, Referenzkunde."
            },
            new MarketingContact
            {
                Email = "keineinteresse@example.at",
                Name = "Max Mustermann",
                ContactType = MarketingContactType.Other,
                Status = MarketingContactStatus.NotInterested,
                Source = "Versand",
                LastContactedAt = DateTimeOffset.UtcNow.AddDays(-20),
                Notes = "Kein Bedarf, nicht erneut kontaktieren."
            }
        );

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
