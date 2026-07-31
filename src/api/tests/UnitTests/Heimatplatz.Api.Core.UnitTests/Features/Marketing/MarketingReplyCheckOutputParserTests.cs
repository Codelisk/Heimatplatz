using FluentAssertions;
using Heimatplatz.Api.Features.Marketing.Services;
using Heimatplatz.Api.UnitTests.Infrastructure;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.Marketing;

/// <summary>
/// Der Parser muss das JSON-Ausgabeformat der Entwurfs-Pruefung robust lesen:
/// KI-Ausgaben kommen mal nackt, mal in Codezaeunen, mal mit Text drumherum;
/// correctedText/suggestedText sind optional (null oder leer = "passt so").
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Fast)]
public class MarketingReplyCheckOutputParserTests : BaseApiUnitTest
{
    [Test]
    public void Parse_PlainJsonWithAllFields_ReturnsCheck()
    {
        var check = MarketingReplyCheckOutputParser.Parse(
            """{"fitsContext": true, "contextNote": "Passt.", "correctedText": "Korrigiert.", "suggestedText": "Vorschlag."}""");

        check.FitsContext.Should().BeTrue();
        check.ContextNote.Should().Be("Passt.");
        check.CorrectedText.Should().Be("Korrigiert.");
        check.SuggestedText.Should().Be("Vorschlag.");
    }

    [Test]
    public void Parse_NullOptionalFields_ReturnsNulls()
    {
        var check = MarketingReplyCheckOutputParser.Parse(
            """{"fitsContext": false, "contextNote": "Frage nach dem Energieausweis bleibt unbeantwortet.", "correctedText": null, "suggestedText": null}""");

        check.FitsContext.Should().BeFalse();
        check.ContextNote.Should().Contain("Energieausweis");
        check.CorrectedText.Should().BeNull();
        check.SuggestedText.Should().BeNull();
    }

    [Test]
    public void Parse_EmptyOptionalFields_CountAsNull()
    {
        var check = MarketingReplyCheckOutputParser.Parse(
            """{"fitsContext": true, "contextNote": "Passt.", "correctedText": "  ", "suggestedText": ""}""");

        check.CorrectedText.Should().BeNull();
        check.SuggestedText.Should().BeNull();
    }

    [Test]
    public void Parse_JsonInCodeFenceWithSurroundingText_ReturnsCheck()
    {
        const string raw = """
            Hier das Ergebnis der Pruefung:
            ```json
            {"fitsContext": true, "contextNote": "Passt gut zum Verlauf."}
            ```
            """;

        var check = MarketingReplyCheckOutputParser.Parse(raw);

        check.FitsContext.Should().BeTrue();
        check.ContextNote.Should().Be("Passt gut zum Verlauf.");
    }

    [Test]
    public void Parse_CaseInsensitivePropertyNames_ReturnsCheck()
    {
        var check = MarketingReplyCheckOutputParser.Parse(
            """{"FitsContext": true, "CONTEXTNOTE": "Passt.", "CorrectedText": "Korrigiert."}""");

        check.FitsContext.Should().BeTrue();
        check.CorrectedText.Should().Be("Korrigiert.");
    }

    [Test]
    public void Parse_MissingContextNote_Throws()
    {
        var act = () => MarketingReplyCheckOutputParser.Parse("""{"fitsContext": true}""");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Kontext-Einschätzung*");
    }

    [Test]
    public void Parse_MissingFitsContext_Throws()
    {
        var act = () => MarketingReplyCheckOutputParser.Parse("""{"contextNote": "Passt."}""");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Kontext-Einschätzung*");
    }

    [Test]
    public void Parse_NoJsonObject_Throws()
    {
        var act = () => MarketingReplyCheckOutputParser.Parse("Der Entwurf passt so.");

        act.Should().Throw<InvalidOperationException>().WithMessage("*kein JSON-Objekt*");
    }

    [Test]
    public void Parse_InvalidJson_Throws()
    {
        var act = () => MarketingReplyCheckOutputParser.Parse("""{"fitsContext": true, "contextNote": }""");

        act.Should().Throw<InvalidOperationException>().WithMessage("*kein gültiges JSON*");
    }
}
