using FluentAssertions;
using FluentAssertions.Primitives;

namespace Heimatplatz.UnitTests.Extensions;

/// <summary>
/// Erweiterungsmethoden fuer FluentAssertions.
/// </summary>
public static class AssertionExtensions
{
    /// <summary>
    /// Prueft ob ein String nicht leer oder null ist.
    /// </summary>
    public static AndConstraint<StringAssertions> NotBeNullOrWhiteSpace(
        this StringAssertions assertions,
        string because = "",
        params object[] becauseArgs)
    {
        return assertions.Subject.Should().NotBeNullOrWhiteSpace(because, becauseArgs);
    }
}
