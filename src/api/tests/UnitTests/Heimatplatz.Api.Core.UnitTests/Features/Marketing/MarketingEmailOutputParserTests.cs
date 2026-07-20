using FluentAssertions;
using Heimatplatz.Api.Features.Marketing.Services;
using Heimatplatz.Api.UnitTests.Infrastructure;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.Marketing;

/// <summary>
/// Der Parser muss das JSON-Ausgabeformat der Marketing-E-Mail-Section robust lesen:
/// KI-Ausgaben kommen mal nackt, mal in Codezaeunen, mal mit Text drumherum.
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Fast)]
public class MarketingEmailOutputParserTests : BaseApiUnitTest
{
    [Test]
    public void Parse_PlainJson_ReturnsDraft()
    {
        var draft = MarketingEmailOutputParser.Parse("""{"subject": "Neues von Heimatplatz", "body": "Guten Tag,\n\nText."}""");

        draft.Subject.Should().Be("Neues von Heimatplatz");
        draft.Body.Should().Be("Guten Tag,\n\nText.");
    }

    [Test]
    public void Parse_JsonInCodeFenceWithSurroundingText_ReturnsDraft()
    {
        const string raw = """
            Hier ist der gewuenschte Entwurf:
            ```json
            {"subject": "Betreff", "body": "Text"}
            ```
            """;

        var draft = MarketingEmailOutputParser.Parse(raw);

        draft.Subject.Should().Be("Betreff");
        draft.Body.Should().Be("Text");
    }

    [Test]
    public void Parse_CaseInsensitivePropertyNames_ReturnsDraft()
    {
        var draft = MarketingEmailOutputParser.Parse("""{"Subject": "Betreff", "BODY": "Text"}""");

        draft.Subject.Should().Be("Betreff");
        draft.Body.Should().Be("Text");
    }

    [Test]
    public void Parse_TrimsWhitespaceAroundValues()
    {
        var draft = MarketingEmailOutputParser.Parse("""{"subject": "  Betreff  ", "body": "  Text  "}""");

        draft.Subject.Should().Be("Betreff");
        draft.Body.Should().Be("Text");
    }

    [Test]
    public void Parse_MissingBody_Throws()
    {
        var act = () => MarketingEmailOutputParser.Parse("""{"subject": "Nur Betreff"}""");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Betreff*");
    }

    [Test]
    public void Parse_NoJsonObject_Throws()
    {
        var act = () => MarketingEmailOutputParser.Parse("Sehr geehrte Damen und Herren, ...");

        act.Should().Throw<InvalidOperationException>().WithMessage("*kein JSON-Objekt*");
    }

    [Test]
    public void Parse_InvalidJson_Throws()
    {
        var act = () => MarketingEmailOutputParser.Parse("""{"subject": "Betreff", "body": }""");

        act.Should().Throw<InvalidOperationException>().WithMessage("*kein gültiges JSON*");
    }
}
