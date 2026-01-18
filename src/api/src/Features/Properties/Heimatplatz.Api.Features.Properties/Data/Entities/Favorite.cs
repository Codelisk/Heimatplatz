using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Auth.Data.Entities;

namespace Heimatplatz.Api.Features.Properties.Data.Entities;

/// <summary>
/// Favorite entity - represents a user's favorited property
/// </summary>
public class Favorite : BaseEntity
{
    /// <summary>ID of the user who favorited the property</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation property to the user</summary>
    public User User { get; set; } = null!;

    /// <summary>ID of the favorited property</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Navigation property to the property</summary>
    public Property Property { get; set; } = null!;
}
