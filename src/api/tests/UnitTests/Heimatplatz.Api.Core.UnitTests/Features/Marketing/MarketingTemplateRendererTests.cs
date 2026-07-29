using FluentAssertions;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Heimatplatz.Api.Features.Marketing.Services;
using Heimatplatz.Api.UnitTests.Infrastructure;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.Marketing;

/// <summary>
/// Der Renderer ist die einzige Stelle mit Anrede-Logik und muss deterministisch sein:
/// strukturierte Namensteile ergeben die formelle Anrede, fehlende Daten fuehren NIE zu
/// stumm leeren Luecken (Platzhalter bleibt stehen + Warning), unbekannte Platzhalter
/// bleiben unangetastet.
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Fast)]
public class MarketingTemplateRendererTests : BaseApiUnitTest
{
    private readonly MarketingTemplateRenderer renderer = new();

    [Test]
    public void Render_HerrWithTitleAndLastName_UsesFormalSalutationWithLastNameOnly()
    {
        var result = renderer.Render(
            Template("{anrede},"),
            Contact(salutation: MarketingSalutation.Herr, title: "Mag.", firstName: "Thomas", lastName: "Kaindl"));

        result.Body.Should().Be("Sehr geehrter Herr Mag. Kaindl,");
        result.Warnings.Should().BeEmpty();
    }

    [Test]
    public void Render_FrauWithLastName_UsesFemaleForm()
    {
        var result = renderer.Render(
            Template("{anrede},"),
            Contact(salutation: MarketingSalutation.Frau, lastName: "Denich-Kobula"));

        result.Body.Should().Be("Sehr geehrte Frau Denich-Kobula,");
    }

    [Test]
    public void Render_UnknownSalutationWithFullName_FallsBackToNeutralGreetingWithFullName()
    {
        var result = renderer.Render(
            Template("{anrede},"),
            Contact(title: "DI", firstName: "Anton", lastName: "Schwarzmayr"));

        result.Body.Should().Be("Guten Tag DI Anton Schwarzmayr,");
    }

    [Test]
    public void Render_LegacyNameOnly_KeepsWorkingWithAllSalutations()
    {
        var legacyHerr = renderer.Render(
            Template("{anrede},"),
            Contact(salutation: MarketingSalutation.Herr, legacyName: "Mag. Thomas Kaindl"));
        var legacyNeutral = renderer.Render(
            Template("{anrede},"),
            Contact(legacyName: "Alexander Huber"));

        // Alt-Bestand hat keinen separaten Nachnamen - voller Name ist laenger, aber nie falsch
        legacyHerr.Body.Should().Be("Sehr geehrter Herr Mag. Thomas Kaindl,");
        legacyNeutral.Body.Should().Be("Guten Tag Alexander Huber,");
    }

    [Test]
    public void Render_NoContactPerson_FallsBackToDamenUndHerrenWithWarning()
    {
        var result = renderer.Render(Template("{anrede},"), Contact(company: "ALVARIUM Immobilien GmbH"));

        result.Body.Should().Be("Sehr geehrte Damen und Herren,");
        result.Warnings.Should().ContainSingle(w => w.Contains("keine Ansprechperson"));
    }

    [Test]
    public void Render_NoContact_FallsBackAndLeavesDataPlaceholdersIntact()
    {
        var result = renderer.Render(Template("{anrede},\n\nFirma {firma} in {ort}."), null);

        result.Body.Should().Be("Sehr geehrte Damen und Herren,\n\nFirma {firma} in {ort}.");
        result.Warnings.Should().HaveCount(3);
    }

    [Test]
    public void Render_MissingCompany_LeavesPlaceholderVisibleWithWarning()
    {
        var result = renderer.Render(
            Template("Anbieter wie {firma} profitieren."),
            Contact(salutation: MarketingSalutation.Herr, lastName: "Huber"));

        // Kein stummes Leer-Ersetzen: "Anbieter wie  profitieren" war die gefaehrlichste
        // Fehlerquelle des alten Renderers
        result.Body.Should().Be("Anbieter wie {firma} profitieren.");
        result.Warnings.Should().ContainSingle(w => w.StartsWith("{firma}"));
    }

    [Test]
    public void Render_UnknownPlaceholder_StaysUntouchedWithWarning()
    {
        var result = renderer.Render(
            Template("Hallo {fimra}!"),
            Contact(company: "AWZ Immo-Invest GmbH"));

        result.Body.Should().Be("Hallo {fimra}!");
        result.Warnings.Should().ContainSingle(w => w.Contains("{fimra}"));
    }

    [Test]
    public void Render_IsCaseAndWhitespaceTolerant()
    {
        var result = renderer.Render(
            Template("{Anrede}, Gruesse an { FIRMA }."),
            Contact(salutation: MarketingSalutation.Frau, lastName: "Denich-Kobula", company: "Denich-Real"));

        result.Body.Should().Be("Sehr geehrte Frau Denich-Kobula, Gruesse an Denich-Real.");
    }

    [Test]
    public void Render_SubjectIsRenderedToo()
    {
        var result = renderer.Render(
            new MarketingEmailTemplate { Name = "t", Subject = "Heimatplatz für {firma}", Body = "x" },
            Contact(company: "Kaindl Real"));

        result.Subject.Should().Be("Heimatplatz für Kaindl Real");
    }

    [Test]
    public void Render_NamePlaceholder_PrefersStructuredPartsOverLegacyName()
    {
        var structured = renderer.Render(
            Template("{name}"),
            Contact(title: "Mag.", firstName: "Thomas", lastName: "Kaindl", legacyName: "Altbestand"));
        var legacy = renderer.Render(Template("{name}"), Contact(legacyName: "Alexander Huber"));

        structured.Body.Should().Be("Mag. Thomas Kaindl");
        legacy.Body.Should().Be("Alexander Huber");
    }

    [Test]
    public void Render_DuplicatePlaceholderInSubjectAndBody_WarnsOnlyOnce()
    {
        var result = renderer.Render(
            new MarketingEmailTemplate { Name = "t", Subject = "Für {firma}", Body = "Nochmal {firma}." },
            Contact(salutation: MarketingSalutation.Herr, lastName: "Huber"));

        result.Warnings.Should().ContainSingle();
    }

    [Test]
    public void FindTokens_MatchesOnlyLetterTokens()
    {
        var tokens = MarketingTemplatePlaceholders.FindTokens("{anrede} {12} {a b} {x} {öffnung}");

        tokens.Should().HaveCount(2);
        tokens[0].Should().Be(new PlaceholderToken("{anrede}", "{anrede}", true));
        tokens[1].IsKnown.Should().BeFalse();
    }

    private static MarketingEmailTemplate Template(string body) =>
        new() { Name = "Test", Subject = "Betreff", Body = body };

    private static MarketingContact Contact(
        MarketingSalutation salutation = MarketingSalutation.Unknown,
        string? title = null,
        string? firstName = null,
        string? lastName = null,
        string? legacyName = null,
        string? company = null,
        string? city = null) =>
        new()
        {
            Salutation = salutation,
            Title = title,
            FirstName = firstName,
            LastName = lastName,
            Name = legacyName,
            Company = company,
            City = city
        };
}
