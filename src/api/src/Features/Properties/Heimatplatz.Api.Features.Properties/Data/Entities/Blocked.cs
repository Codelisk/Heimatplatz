using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Auth.Data.Entities;

namespace Heimatplatz.Api.Features.Properties.Data.Entities;

/// <summary>
/// Blocked entity - represents a user's blocked property
/// Blocked properties are hidden from the main property list for this user
/// </summary>
public class Blocked : BaseEntity
{
    /// <summary>ID of the user who blocked the property</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation property to the user</summary>
    public User User { get; set; } = null!;

    /// <summary>ID of the blocked property</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Navigation property to the property</summary>
    public Property Property { get; set; } = null!;
}
