using Heimatplatz.Maui.ApiClient.Generated;

namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Anzeige-Wrapper fuer einen Inserat-Entwurf in "Meine Immobilien"
/// (das generierte DTO liegt in einer anderen Assembly und kann keine
/// berechneten Anzeige-Properties tragen). DisplayTitle und StepText
/// liefert das konstruierende ViewModel bereits lokalisiert mit
/// (Models haben kein DI und damit keinen Zugriff auf Loc).
/// </summary>
public record DraftListItem(PropertyDraftListItemDto Dto, string DisplayTitle, string StepText)
{
    public Guid Id => Dto.Id;

    public bool HasImage => !string.IsNullOrEmpty(Dto.FirstImageUrl);

    public ImageSource? Thumbnail => HasImage
        ? ImageSource.FromUri(new Uri(Dto.FirstImageUrl!))
        : null;
}
