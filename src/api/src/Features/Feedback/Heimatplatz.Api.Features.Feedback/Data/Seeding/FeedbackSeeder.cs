using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Feedback.Data.Seeding;

/// <summary>
/// Demo-Anfragen fuer Entwicklung/Test-System (IsDemoData=true, laeuft nie in Produktion).
/// Haengt an den Seed-Usern - ohne test.buyer wird nichts geseedet.
/// </summary>
public class FeedbackSeeder(AppDbContext dbContext) : ISeeder
{
    /// <summary>Nach den Auth-Seed-Usern (deren IDs werden referenziert)</summary>
    public int Order => 30;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<FeedbackTicket>().AnyAsync(cancellationToken))
            return;

        var buyer = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == "test.buyer@heimatplatz.dev", cancellationToken);
        var seller = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == "test.seller@heimatplatz.dev", cancellationToken);

        if (buyer == null)
            return;

        var now = DateTimeOffset.UtcNow;

        var wish = new FeedbackTicket
        {
            UserId = buyer.Id,
            Category = FeedbackCategory.Idea,
            Subject = "Kartenansicht für die Suche",
            Status = FeedbackTicketStatus.Answered,
            Source = FeedbackSource.Android,
            AppVersion = "1.80.0",
            LastMessageAt = now.AddDays(-1),
            HasUnreadForUser = true,
            Messages =
            {
                new FeedbackMessage
                {
                    Author = FeedbackAuthor.User,
                    Body = "Es wäre super, wenn man die Ergebnisse auch auf einer Karte sehen könnte. Gerade bei Grundstücken hilft die Lage oft mehr als Fotos.",
                    CreatedAt = now.AddDays(-3)
                },
                new FeedbackMessage
                {
                    Author = FeedbackAuthor.Team,
                    Body = "Danke für die Idee! Eine Kartenansicht steht bei uns auf der Liste - wir melden uns, sobald es Neuigkeiten gibt.",
                    CreatedAt = now.AddDays(-1)
                }
            }
        };

        var problem = new FeedbackTicket
        {
            UserId = buyer.Id,
            Category = FeedbackCategory.Problem,
            Subject = "Fotos laden langsam im WLAN",
            Status = FeedbackTicketStatus.Open,
            Source = FeedbackSource.Ios,
            AppVersion = "1.80.0",
            LastMessageAt = now.AddHours(-5),
            HasUnreadForTeam = true,
            Messages =
            {
                new FeedbackMessage
                {
                    Author = FeedbackAuthor.User,
                    Body = "Seit dem letzten Update dauern die Fotos in der Liste bei mir spürbar länger. Passiert nur im Heim-WLAN, mobil ist alles normal.",
                    CreatedAt = now.AddHours(-5)
                }
            }
        };

        dbContext.Set<FeedbackTicket>().AddRange(wish, problem);

        if (seller != null)
        {
            dbContext.Set<FeedbackTicket>().Add(new FeedbackTicket
            {
                UserId = seller.Id,
                Category = FeedbackCategory.Question,
                Subject = "Inserat nachträglich bearbeiten?",
                Status = FeedbackTicketStatus.Closed,
                Source = FeedbackSource.Web,
                LastMessageAt = now.AddDays(-6),
                Messages =
                {
                    new FeedbackMessage
                    {
                        Author = FeedbackAuthor.User,
                        Body = "Kann ich bei einem veröffentlichten Inserat später noch Fotos austauschen?",
                        CreatedAt = now.AddDays(-7)
                    },
                    new FeedbackMessage
                    {
                        Author = FeedbackAuthor.Team,
                        Body = "Ja - unter \"Meine Inserate\" > Bearbeiten kannst du jederzeit Fotos ergänzen oder entfernen. Die Änderungen sind sofort online.",
                        CreatedAt = now.AddDays(-6)
                    }
                }
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
