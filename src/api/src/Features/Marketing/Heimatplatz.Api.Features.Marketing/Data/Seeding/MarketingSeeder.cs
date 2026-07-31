using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Marketing.Data.Seeding;

/// <summary>
/// Demo-Kontakte fuer Entwicklung/Test-System (IsDemoData=true, laeuft nie in Produktion).
/// Zwei Kontakte (Gruber, Steiner) bekommen einen kompletten Gespraechsverlauf aus
/// Versand, Rueckmeldung und Aktivitaeten - damit der Chat-Verlauf der Detailseite
/// ohne echtes Postfach sichtbar ist.
/// </summary>
public class MarketingSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 20;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<MarketingContact>().AnyAsync(cancellationToken))
            return;

        var gruber = new MarketingContact
        {
            Email = "p.gruber@example.at",
            Name = "Peter Gruber",
            ContactType = MarketingContactType.PrivateSeller,
            Status = MarketingContactStatus.Replied,
            Source = "Versand",
            LastContactedAt = DateTimeOffset.UtcNow.AddDays(-10),
            LastReplyAt = DateTimeOffset.UtcNow.AddDays(-8),
            Notes = "Will sein Elternhaus in Ohlsdorf verkaufen, meldet sich im Herbst."
        };

        var steiner = new MarketingContact
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
        };

        dbContext.Set<MarketingContact>().AddRange(
            gruber,
            steiner,
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

        // Gespraechsverlauf Gruber: Erstansprache -> lange Rueckmeldung -> unsere
        // Antwort -> Telefonat mit Wiedervorlage. Die lange Rueckmeldung prueft das
        // "Mehr anzeigen"-Clamping im Chat-Verlauf.
        var gruberErstmail = new MarketingEmail
        {
            Contact = gruber,
            Subject = "Ihr Haus kostenlos auf Heimatplatz inserieren",
            Body = "Sehr geehrter Herr Gruber,\n\n"
                + "Heimatplatz ist der Immobilienmarktplatz für Oberösterreich - Häuser und "
                + "Grundstücke, komplett kostenlos für private Verkäufer.\n\n"
                + "Gerne unterstützen wir Sie beim Inserat Ihres Hauses in Ohlsdorf.\n\n"
                + "Freundliche Grüße\nDaniel Hufnagl",
            MessageId = "demo-gruber-erstmail@heimatplatz.at",
            Status = MarketingEmailStatus.Sent,
            SentAt = DateTimeOffset.UtcNow.AddDays(-10)
        };
        var gruberAntwortmail = new MarketingEmail
        {
            Contact = gruber,
            Subject = "Re: Ihr Haus kostenlos auf Heimatplatz inserieren",
            Body = "Sehr geehrter Herr Gruber,\n\n"
                + "vielen Dank für Ihre Rückmeldung - gerne melde ich mich im Herbst wieder. "
                + "Wenn Sie vorab Fragen haben, erreichen Sie mich jederzeit unter dieser Adresse.\n\n"
                + "Freundliche Grüße\nDaniel Hufnagl",
            MessageId = "demo-gruber-antwort@heimatplatz.at",
            Status = MarketingEmailStatus.Sent,
            SentAt = DateTimeOffset.UtcNow.AddDays(-8).AddHours(3)
        };
        dbContext.Set<MarketingEmail>().AddRange(gruberErstmail, gruberAntwortmail);

        dbContext.Set<MarketingInboundEmail>().Add(new MarketingInboundEmail
        {
            Contact = gruber,
            RepliedToEmail = gruberErstmail,
            FromAddress = "p.gruber@example.at",
            FromName = "Peter Gruber",
            Subject = "Re: Ihr Haus kostenlos auf Heimatplatz inserieren",
            BodyText = "Guten Tag,\n\n"
                + "danke für Ihre Nachricht. Das Haus meiner Eltern in Ohlsdorf steht tatsächlich "
                + "zum Verkauf, allerdings erst im Herbst - vorher sind noch ein paar Dinge zu "
                + "erledigen:\n\n"
                + "- Die Verlassenschaft ist noch nicht ganz abgeschlossen\n"
                + "- Der Dachboden muss ausgeräumt werden\n"
                + "- Ein Energieausweis fehlt noch\n\n"
                + "Können Sie mir schon einmal schicken, welche Unterlagen ich für ein Inserat "
                + "brauche? Und ist das Inserieren wirklich kostenlos, auch mit mehreren Fotos?\n\n"
                + "Am besten erreichen Sie mich abends telefonisch.\n\n"
                + "Mit freundlichen Grüßen\nPeter Gruber",
            MessageId = "demo-gruber-reply@example.at",
            InReplyTo = "demo-gruber-erstmail@heimatplatz.at",
            ReceivedAt = DateTimeOffset.UtcNow.AddDays(-8),
            IsRead = true
        });

        dbContext.Set<MarketingActivity>().AddRange(
            new MarketingActivity
            {
                Contact = gruber,
                Type = MarketingActivityType.Call,
                Notes = "Abends erreicht - sehr freundlich. Haus ca. 140 m², Garten, "
                    + "will im Oktober inserieren. Unterlagen-Checkliste per Mail zugesagt.",
                FollowUpAt = DateTimeOffset.UtcNow.AddDays(45),
                OccurredAt = DateTimeOffset.UtcNow.AddDays(-7)
            },
            MarketingActivity.StatusChange(
                gruber.Id,
                MarketingContactStatus.Contacted,
                MarketingContactStatus.Replied,
                DateTimeOffset.UtcNow.AddDays(-8)));

        // Gespraechsverlauf Steiner: Erstansprache -> kurze Rueckmeldung, noch offen
        var steinerErstmail = new MarketingEmail
        {
            Contact = steiner,
            Subject = "Heimatplatz für Steiner Immobilien - kostenlose Inserate",
            Body = "Sehr geehrte Frau Steiner,\n\n"
                + "auf Heimatplatz inserieren Makler aus Oberösterreich ihre Häuser und "
                + "Grundstücke derzeit kostenlos. Gerne zeige ich Ihnen in einem kurzen "
                + "Telefonat, wie Sie Ihre Objekte einstellen.\n\n"
                + "Freundliche Grüße\nDaniel Hufnagl",
            MessageId = "demo-steiner-erstmail@heimatplatz.at",
            Status = MarketingEmailStatus.Sent,
            SentAt = DateTimeOffset.UtcNow.AddDays(-15)
        };
        dbContext.Set<MarketingEmail>().Add(steinerErstmail);

        dbContext.Set<MarketingInboundEmail>().Add(new MarketingInboundEmail
        {
            Contact = steiner,
            RepliedToEmail = steinerErstmail,
            FromAddress = "office@steiner-immobilien.example.at",
            FromName = "Karin Steiner",
            Subject = "Re: Heimatplatz für Steiner Immobilien - kostenlose Inserate",
            BodyText = "Sehr geehrter Herr Hufnagl,\n\n"
                + "das klingt interessant. Schicken Sie mir bitte Details zum Ablauf - "
                + "ein Telefontermin nächste Woche passt mir gut, am besten vormittags.\n\n"
                + "Beste Grüße\nKarin Steiner",
            MessageId = "demo-steiner-reply@example.at",
            InReplyTo = "demo-steiner-erstmail@heimatplatz.at",
            ReceivedAt = DateTimeOffset.UtcNow.AddDays(-13),
            IsRead = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
