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

[TestFixture]
public class DashboardDefinitionValidatorTests
{
    private static DashboardDefinitionValidator CreateValidator(DashboardOptions? options = null)
    {
        var opts = Options.Create(options ?? new DashboardOptions());
        var mediator = Substitute.For<IMediator>();

        // Ohne locations in den Queries ruft der Validator den Mediator nie auf -
        // die Orts-Aufloesung selbst wird ueber ResolveLocationName statisch getestet.
        IDashboardWidgetResolver[] resolvers =
        [
            new StatRowWidgetResolver(mediator, opts),
            new PropertyListWidgetResolver(mediator, opts),
            new MapWidgetResolver(mediator, opts),
            new HighlightWidgetResolver(mediator, opts),
            new NewListingsWidgetResolver(mediator, opts),
            new TextNoteWidgetResolver()
        ];

        return new DashboardDefinitionValidator(
            resolvers, mediator, opts, NullLogger<DashboardDefinitionValidator>.Instance);
    }

    private static DashboardWidget Widget(string kind, DashboardWidgetOptions? options = null) => new()
    {
        Id = "x",
        Kind = kind,
        Query = new DashboardPropertyQuery(),
        Options = options
    };

    [Test]
    public async Task Validate_DropsUnknownKindsAndReassignsIds()
    {
        var validator = CreateValidator();
        var definition = new DashboardDefinition
        {
            Title = "Test",
            Widgets =
            [
                Widget("property-list"),
                Widget("chart-3d"),
                Widget("map")
            ]
        };

        var result = await validator.ValidateAsync(definition, DashboardViewTypes.Dashboard, CancellationToken.None);

        result.Widgets.Should().HaveCount(2);
        result.Widgets.Select(w => w.Kind).Should().Equal("property-list", "map");
        result.Widgets.Select(w => w.Id).Should().Equal("w1", "w2");
    }

    [Test]
    public async Task Validate_NormalizesKindCasingAndSizes()
    {
        var validator = CreateValidator();
        var definition = new DashboardDefinition
        {
            Title = "Test",
            Widgets = [new DashboardWidget { Kind = "Property-List", Size = "riesig" }]
        };

        var result = await validator.ValidateAsync(definition, DashboardViewTypes.Dashboard, CancellationToken.None);

        result.Widgets[0].Kind.Should().Be("property-list");
        result.Widgets[0].Size.Should().Be(DashboardWidgetSizes.L);
        result.Widgets[0].Query.Should().NotBeNull("property-list bekommt eine Default-Query");
        result.Widgets[0].Query!.Limit.Should().Be(PropertyListWidgetResolver.DefaultLimit);
    }

    [Test]
    public async Task Validate_DropsTextNoteWithoutText()
    {
        var validator = CreateValidator();
        var definition = new DashboardDefinition
        {
            Title = "Test",
            Widgets =
            [
                Widget("text-note"),
                Widget("text-note", new DashboardWidgetOptions { Text = "  Hallo!  " })
            ]
        };

        var result = await validator.ValidateAsync(definition, DashboardViewTypes.Dashboard, CancellationToken.None);

        result.Widgets.Should().HaveCount(1);
        result.Widgets[0].Options!.Text.Should().Be("Hallo!");
        result.Widgets[0].Query.Should().BeNull();
    }

    [Test]
    public async Task Validate_CapsWidgetCountAtLimit()
    {
        var validator = CreateValidator(new DashboardOptions { Limits = { MaxWidgets = 2 } });
        var definition = new DashboardDefinition
        {
            Title = "Test",
            Widgets = [Widget("map"), Widget("map"), Widget("map")]
        };

        var result = await validator.ValidateAsync(definition, DashboardViewTypes.Dashboard, CancellationToken.None);

        result.Widgets.Should().HaveCount(2);
    }

    [Test]
    public async Task Validate_FallsBackToDefaultTitleAndCapsWishes()
    {
        var validator = CreateValidator();
        var definition = new DashboardDefinition
        {
            Title = "   ",
            Widgets = [Widget("map")],
            UnsupportedWishes = Enumerable.Range(1, 15).Select(i => $"Wunsch {i}").ToList()
        };

        var result = await validator.ValidateAsync(definition, DashboardViewTypes.Dashboard, CancellationToken.None);

        result.Title.Should().Be("Meine Übersicht");
        result.UnsupportedWishes.Should().HaveCount(10);
    }

    [Test]
    public async Task Validate_ThrowsWhenNothingSurvives()
    {
        var validator = CreateValidator();
        var definition = new DashboardDefinition
        {
            Title = "Test",
            Widgets = [Widget("hologram"), Widget("text-note")]
        };

        var act = () => validator.ValidateAsync(definition, DashboardViewTypes.Dashboard, CancellationToken.None);

        await act.Should().ThrowAsync<DashboardValidationException>()
            .WithMessage(DashboardDefinitionValidator.NoWidgetsMessage);
    }

    [Test]
    public async Task Validate_ThrowsOnWrongSchemaVersion()
    {
        var validator = CreateValidator();
        var definition = new DashboardDefinition { SchemaVersion = 99, Widgets = [Widget("map")] };

        var act = () => validator.ValidateAsync(definition, DashboardViewTypes.Dashboard, CancellationToken.None);

        await act.Should().ThrowAsync<DashboardValidationException>();
    }

    [Test]
    public async Task Validate_ListViewKeepsExactlyOneFullWidthList()
    {
        var validator = CreateValidator();
        var definition = new DashboardDefinition
        {
            Title = "Test",
            Widgets =
            [
                Widget("map"),
                new DashboardWidget { Kind = "property-list", Size = "s", Query = new DashboardPropertyQuery() },
                Widget("property-list"),
                Widget("stat-row")
            ]
        };

        var result = await validator.ValidateAsync(definition, DashboardViewTypes.List, CancellationToken.None);

        result.Widgets.Should().HaveCount(1);
        result.Widgets[0].Kind.Should().Be("property-list");
        result.Widgets[0].Size.Should().Be(DashboardWidgetSizes.Full);
        result.Widgets[0].Query!.Limit.Should().Be(12, "seitenfuellende Listen bekommen einen hoeheren Default");
    }

    [Test]
    public async Task Validate_KeepsExplicitFilterHiddenAndDropsEverythingElse()
    {
        var validator = CreateValidator();
        var hidden = new DashboardDefinition
        {
            Title = "Test",
            Widgets = [Widget("property-list")],
            Filter = new DashboardFilterSpec { Hidden = true }
        };
        var noise = new DashboardDefinition
        {
            Title = "Test",
            Widgets = [Widget("property-list")],
            Filter = new DashboardFilterSpec { Hidden = false }
        };

        var hiddenResult = await validator.ValidateAsync(hidden, DashboardViewTypes.List, CancellationToken.None);
        var noiseResult = await validator.ValidateAsync(noise, DashboardViewTypes.List, CancellationToken.None);

        hiddenResult.Filter.Should().NotBeNull();
        hiddenResult.Filter!.Hidden.Should().BeTrue();
        noiseResult.Filter.Should().BeNull("nur die dokumentierte Abweichung hidden=true bleibt erhalten");
    }

    [Test]
    public async Task Validate_ForcesHighlightLimitToOne()
    {
        var validator = CreateValidator();
        var definition = new DashboardDefinition
        {
            Title = "Test",
            Widgets =
            [
                new DashboardWidget
                {
                    Kind = "highlight",
                    Query = new DashboardPropertyQuery { Limit = 12, Sort = "price-asc" }
                }
            ]
        };

        var result = await validator.ValidateAsync(definition, DashboardViewTypes.Dashboard, CancellationToken.None);

        result.Widgets[0].Query!.Limit.Should().Be(1);
        result.Widgets[0].Query!.Sort.Should().Be("price-asc");
    }
}
