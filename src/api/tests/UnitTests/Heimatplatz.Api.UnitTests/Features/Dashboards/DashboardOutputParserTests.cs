using FluentAssertions;
using Heimatplatz.Api.Features.Dashboards.Services;
using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Features.Dashboards;

[TestFixture]
public class DashboardOutputParserTests
{
    const string ValidJson = """
        {"schemaVersion":1,"title":"Test","widgets":[{"id":"w1","kind":"property-list","size":"l"}]}
        """;

    [Test]
    public void Parse_ReadsPlainJson()
    {
        var definition = DashboardOutputParser.Parse(ValidJson);

        definition.Title.Should().Be("Test");
        definition.Widgets.Should().HaveCount(1);
        definition.Widgets[0].Kind.Should().Be("property-list");
    }

    [Test]
    public void Parse_StripsMarkdownFences()
    {
        var definition = DashboardOutputParser.Parse($"```json\n{ValidJson}\n```");

        definition.Title.Should().Be("Test");
    }

    [Test]
    public void Parse_IgnoresSurroundingText()
    {
        var definition = DashboardOutputParser.Parse($"Hier ist Ihre Übersicht:\n{ValidJson}\nViel Erfolg!");

        definition.Title.Should().Be("Test");
        definition.Widgets.Should().HaveCount(1);
    }

    [Test]
    public void Parse_ReadsCaseInsensitivePropertyNames()
    {
        var definition = DashboardOutputParser.Parse(
            """{"SchemaVersion":1,"TITLE":"Gross","Widgets":[]}""");

        definition.Title.Should().Be("Gross");
    }

    [Test]
    public void Parse_ThrowsWithoutJsonObject()
    {
        var act = () => DashboardOutputParser.Parse("Leider kann ich das nicht.");

        act.Should().Throw<InvalidOperationException>().WithMessage("*kein JSON-Objekt*");
    }

    [Test]
    public void Parse_ThrowsOnBrokenJson()
    {
        var act = () => DashboardOutputParser.Parse("""{"schemaVersion":1,"widgets":[{}""");

        act.Should().Throw<InvalidOperationException>().WithMessage("*kein gueltiges JSON*");
    }
}
