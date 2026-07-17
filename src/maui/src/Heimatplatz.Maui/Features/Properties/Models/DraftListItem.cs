using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Anzeige-Wrapper fuer einen Inserat-Entwurf in "Meine Immobilien"
/// (das generierte DTO liegt in einer anderen Assembly und kann keine
/// berechneten Anzeige-Properties tragen).
/// </summary>
public record DraftListItem(PropertyDraftListItemDto Dto)
{
    public Guid Id => Dto.Id;

    public string DisplayTitle => string.IsNullOrWhiteSpace(Dto.Title)
        ? $"Entwurf vom {Dto.UpdatedAt.ToLocalTime():dd.MM.yyyy}"
        : Dto.Title!;

    public string StepText =>
        $"Schritt {Dto.StepIndex + 1} von {PropertyWizardViewModel.StepCount} · {Dto.UpdatedAt.ToLocalTime():dd.MM.yyyy HH:mm}";

    public bool HasImage => !string.IsNullOrEmpty(Dto.FirstImageUrl);

    public ImageSource? Thumbnail => HasImage
        ? ImageSource.FromUri(new Uri(Dto.FirstImageUrl!))
        : null;
}
