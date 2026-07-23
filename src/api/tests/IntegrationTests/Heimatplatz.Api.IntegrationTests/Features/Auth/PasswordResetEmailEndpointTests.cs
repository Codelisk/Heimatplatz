using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Heimatplatz.Api.Core.Email;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Features.Auth;

/// <summary>
/// End-to-End-Tests fuer den Passwort-Reset-Link und seine Browser-Integration.
/// </summary>
[TestFixture]
[Category(TestCategories.Auth)]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class PasswordResetEmailEndpointTests : BaseApiIntegrationTest
{
    private const string Password = "Passwort123!";
    private readonly CapturingEmailSender emailSender = new();

    protected override WebApplicationFactory<Program> CreateFactory()
        => new EmailCaptureWebApplicationFactory<Program>(emailSender);

    [Test]
    public async Task ForgotPassword_ResetLinkCarriesUsernameInFragmentForPasswordManagers()
    {
        var email = $"password-reset-{Guid.NewGuid():N}@heimatplatz.dev";
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = "Max",
            LastName = "Mustermann",
            Email = email,
            Password
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        emailSender.Messages.Clear();

        var forgotResponse = await Client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            Email = email
        });

        forgotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var message = emailSender.Messages.Should().ContainSingle().Which;
        message.Subject.Should().Be("Passwort zurücksetzen");

        var resetLink = message.TextBody
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.StartsWith("http", StringComparison.Ordinal));
        var resetUri = new Uri(resetLink);

        resetUri.AbsolutePath.Should().Be("/passwort-zuruecksetzen/");
        resetUri.Query.Should().StartWith("?token=");
        resetUri.Fragment.Should().Be($"#email={Uri.EscapeDataString(email)}");
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task<EmailSendResult> SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.FromResult(new EmailSendResult(true, Guid.NewGuid().ToString("N")));
        }
    }

    private sealed class EmailCaptureWebApplicationFactory<TProgram>(IEmailSender emailSender)
        : CustomWebApplicationFactory<TProgram>
        where TProgram : class
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton(emailSender);
        }
    }
}
