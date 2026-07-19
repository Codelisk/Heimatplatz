using FluentAssertions;
using Heimatplatz.Api.Features.Telemetry.Infrastructure;
using Heimatplatz.Api.UnitTests.Infrastructure;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.Telemetry;

/// <summary>
/// Fingerprint-Stabilitaet: gleiche Fehlerursache muss denselben Hash ergeben,
/// auch wenn Build-Pfade, Zeilennummern oder Message-Werte variieren.
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Fast)]
public class ErrorFingerprintServiceTests : BaseApiUnitTest
{
    private readonly ErrorFingerprintService service = new();

    [Test]
    public void Fingerprint_SameStackDifferentPathsAndLineNumbers_SameHash()
    {
        const string stackA = """
               at Heimatplatz.Api.Features.Properties.Handlers.GetPropertyByIdHandler.Handle()
               at Shiny.Mediator.Impl.Invoke() in C:\build\agent1\src\Mediator.cs:line 42
            """;
        const string stackB = """
               at Heimatplatz.Api.Features.Properties.Handlers.GetPropertyByIdHandler.Handle()
               at Shiny.Mediator.Impl.Invoke() in /home/deploy/src/Mediator.cs:line 99
            """;

        var hashA = service.Fingerprint("System.InvalidOperationException", stackA, "Fehler bei {Id}");
        var hashB = service.Fingerprint("System.InvalidOperationException", stackB, "Fehler bei {Id}");

        hashA.Should().Be(hashB);
        hashA.Should().HaveLength(64);
    }

    [Test]
    public void Fingerprint_DifferentExceptionType_DifferentHash()
    {
        const string stack = "   at Heimatplatz.Api.X.Y()";

        service.Fingerprint("System.InvalidOperationException", stack, null)
            .Should().NotBe(service.Fingerprint("System.NullReferenceException", stack, null));
    }

    [Test]
    public void Fingerprint_DifferentTopFrames_DifferentHash()
    {
        service.Fingerprint("System.InvalidOperationException", "   at A.B()", null)
            .Should().NotBe(service.Fingerprint("System.InvalidOperationException", "   at C.D()", null));
    }

    [Test]
    public void Fingerprint_GermanFramePrefix_SameHashAsEnglish()
    {
        // Deutsche OS-Sprache erzeugt "bei " statt "at " - gleiche Frames, gleicher Hash
        var english = service.Fingerprint("System.Exception", "   at A.B() in C:\\x.cs:line 1", null);
        var german = service.Fingerprint("System.Exception", "   bei A.B() in C:\\x.cs:line 7", null);

        german.Should().Be(english);
    }

    [Test]
    public void ParseExceptionText_TypedFirstLine_ExtractsTypeMessageAndStack()
    {
        var (type, message, stackTrace) = service.ParseExceptionText(
            "System.InvalidOperationException: Kaputt gegangen\n   at Client.App.Boom()");

        type.Should().Be("System.InvalidOperationException");
        message.Should().Be("Kaputt gegangen");
        stackTrace.Should().Contain("Client.App.Boom");
    }

    [Test]
    public void ParseExceptionText_PlainText_FallsBackToClientError()
    {
        var (type, message, stackTrace) = service.ParseExceptionText("Irgendwas ist schiefgelaufen");

        type.Should().Be("ClientError");
        message.Should().Be("Irgendwas ist schiefgelaufen");
        stackTrace.Should().BeNull();
    }

    [Test]
    public void BuildTitle_MultilineMessage_UsesFirstLineOnly()
    {
        service.BuildTitle("System.Exception", "Zeile eins\nZeile zwei")
            .Should().Be("System.Exception: Zeile eins");
    }
}
