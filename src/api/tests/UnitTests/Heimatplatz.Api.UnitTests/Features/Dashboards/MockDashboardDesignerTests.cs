using FluentAssertions;
using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Services;
using Heimatplatz.Api.Features.Dashboards.Services.Widgets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Shiny.Mediator;

namespace Heimatplatz.Api.UnitTests.Features.Dashboards;

/// <summary>
/// Der Mock-Designer laeuft durch dieselbe Parser-/Validator-Pipeline wie echte
/// KI-Antworten - diese Tests garantieren, dass seine Ausgabe die Pipeline
/// vollstaendig uebersteht (sonst waere jeder lokale E2E-Lauf kaputt).
/// </summary>
[TestFixture]
public class MockDashboardDesignerTests
{
    private static MockDashboardDesigner CreateDesigner() =>
        new(Options.Create(new DashboardOptions { MockDelaySeconds = 0 }),
            NullLogger<MockDashboardDesigner>.Instance);

    private static DashboardDefinitionValidator CreateValidator()
    {
        var opts = Options.Create(new DashboardOptions());
        var mediator = Substitute.For<IMediator>();
        IDashboardWidgetResolver[] resolvers =
        [
            new StatRowWidgetResolver(mediator, opts),
            new PropertyListWidgetResolver(mediator, opts),
            new MapWidgetResolver(mediator, opts),
            new HighlightWidgetResolver(mediator, opts),
            new NewListingsWidgetResolver(mediator, opts),
            new PriceChartWidgetResolver(mediator, new Heimatplatz.Api.Features.Dashboards.Services.Charts.DashboardChartRenderer(), opts),
            new TextNoteWidgetResolver()
        ];
        return new DashboardDefinitionValidator(
            resolvers, mediator, opts, NullLogger<DashboardDefinitionValidator>.Instance);
    }

    [Test]
    public async Task InitialDesign_SurvivesFullPipelineWithAllWidgetKinds()
    {
        var raw = await CreateDesigner().DesignAsync("Häuser bis 400.000 Euro mit Karte", null);

        var validated = await CreateValidator().ValidateAsync(
            DashboardOutputParser.Parse(raw), CancellationToken.None);

        validated.Widgets.Should().HaveCount(7, "die Beispiel-Definition deckt alle Katalog-Widgets ab");
        validated.Widgets.Select(w => w.Kind).Should().BeEquivalentTo(
        [
            DashboardWidgetKinds.StatRow,
            DashboardWidgetKinds.PropertyList,
            DashboardWidgetKinds.Map,
            DashboardWidgetKinds.NewListings,
            DashboardWidgetKinds.PriceChart,
            DashboardWidgetKinds.Highlight,
            DashboardWidgetKinds.TextNote
        ]);
        validated.Title.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task RefineDesign_AppendsInstructionNoteToCurrentDefinition()
    {
        var designer = CreateDesigner();
        var validator = CreateValidator();

        var initialRaw = await designer.DesignAsync("Häuser in Linz", null);
        var initial = await validator.ValidateAsync(DashboardOutputParser.Parse(initialRaw), CancellationToken.None);
        var initialJson = System.Text.Json.JsonSerializer.Serialize(
            initial, Heimatplatz.Api.Features.Dashboards.Infrastructure.DashboardDefinitionSerializer.JsonOptions);

        var refinedRaw = await designer.DesignAsync("Mach die Karte größer", initialJson);
        var refined = await validator.ValidateAsync(DashboardOutputParser.Parse(refinedRaw), CancellationToken.None);

        refined.Widgets.Should().HaveCount(initial.Widgets.Count + 1);
        refined.Widgets[^1].Kind.Should().Be(DashboardWidgetKinds.TextNote);
        refined.Widgets[^1].Options!.Text.Should().Contain("Mach die Karte größer");
    }
}
