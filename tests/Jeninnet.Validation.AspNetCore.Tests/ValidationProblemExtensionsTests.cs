//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore.Tests;

[TestClass]
public sealed class ValidationProblemExtensionsTests {
    [TestMethod]
    public void ToProblemErrors_GroupsByMemberAndPreservesOrder() {
        var errors = new[] {
            new ValidationError("Email", new MessageId("Email"), "Email", message: "not an email"),
            new ValidationError("Email", new MessageId("NotEmpty"), "NotEmpty", message: "required"),
            new ValidationError("Name", new MessageId("NotEmpty"), "NotEmpty", message: "required")
        };

        var map = errors.ToProblemErrors();

        MSTestAssertions.That(() => map.ContainsKey("Email").Should().BeTrue());
        MSTestAssertions.That(() => map["Email"].Should().HaveCount(2));
        MSTestAssertions.That(() => map["Email"][0].Should().Be("not an email"));
        MSTestAssertions.That(() => map["Email"][1].Should().Be("required"));
        MSTestAssertions.That(() => map["Name"].Should().HaveCount(1));
    }

    [TestMethod]
    public void ToProblemErrors_FallsBackToMessageKeyWhenNoAuthoredMessageExists() {
        var errors = new[] {
            new ValidationError("Name", new MessageId("NotEmpty"), "NotEmpty")
        };

        var map = errors.ToProblemErrors();

        MSTestAssertions.That(() => map["Name"][0].Should().Be("NotEmpty"));
    }

    [TestMethod]
    public async Task UsesTheMessageLocalizerForKeyAsync() {
        var errors = new[] {
            new ValidationError(
                "Name",
                new MessageId("NotEmpty"),
                "NotEmpty",
                messageParameters: new Dictionary<string, object> { ["PropertyName"] = "Name" })
        };

        var map = await errors.ToProblemErrorsAsync(
            null,
            new FixedMessageLocalizer("Name is required."),
            CultureInfo.InvariantCulture,
            CancellationToken.None);

        MSTestAssertions.That(() => map["Name"][0].Should().Be("Name is required."));
    }

    [TestMethod]
    public void ToProblemErrors_IncludesCodesAndSeveritiesWhenConfigured() {
        var errors = new[] {
            new ValidationError(
                "Email",
                new MessageId("Email"),
                "Email",
                message: "not an email",
                errorCode: "EMAIL_FORMAT",
                severity: ValidationErrorSeverity.Warning)
        };

        var map = errors.ToProblemErrors(new ValidationProblemOptions {
            IncludeErrorCodes = true,
            IncludeSeverities = true
        });

        MSTestAssertions.That(() => map["Email"][0].Should().Be("not an email [EMAIL_FORMAT] (Warning)"));
    }

    [TestMethod]
    public void BuildProblemExtensions_AddsConfiguredDiagnostics() {
        var httpContext = new DefaultHttpContext {
            TraceIdentifier = "trace-1"
        };
        httpContext.Request.Headers["X-Correlation-Id"] = "corr-1";

        var extensions = ValidationProblemExtensions.BuildProblemExtensions(
            httpContext,
            new ValidationProblemOptions {
                IncludeCorrelationId = true,
                CorrelationIdHeader = "X-Correlation-Id",
                IncludeModelType = true,
                IncludeDuration = true
            },
            "My.Model",
            TimeSpan.FromMilliseconds(12.345));

        MSTestAssertions.That(() => extensions["correlationId"].Should().Be("corr-1"));
        MSTestAssertions.That(() => extensions["modelType"].Should().Be("My.Model"));
        MSTestAssertions.That(() => extensions["durationMs"].Should().Be(12.345));
    }

    [TestMethod]
    public void BuildProblemExtensions_FallsBackToTraceIdentifier() {
        var httpContext = new DefaultHttpContext {
            TraceIdentifier = "trace-1"
        };

        var extensions = ValidationProblemExtensions.BuildProblemExtensions(
            httpContext,
            new ValidationProblemOptions { IncludeCorrelationId = true });

        MSTestAssertions.That(() => extensions["correlationId"].Should().Be("trace-1"));
    }

    [TestMethod]
    public void EnrichProblem_AddsExtensionsToTheProblem() {
        var httpContext = new DefaultHttpContext {
            TraceIdentifier = "trace-1"
        };
        var problem = new ValidationProblemDetails();

        ValidationProblemExtensions.EnrichProblem(
            problem,
            httpContext,
            new ValidationProblemOptions {
                IncludeCorrelationId = true,
                IncludeModelType = true,
                IncludeDuration = true
            },
            "My.Model",
            TimeSpan.FromMilliseconds(1));

        MSTestAssertions.That(() => problem.Extensions["correlationId"].Should().Be("trace-1"));
        MSTestAssertions.That(() => problem.Extensions["modelType"].Should().Be("My.Model"));
        MSTestAssertions.That(() => problem.Extensions["durationMs"].Should().Be(1.0));
    }

    private sealed class FixedMessageLocalizer(string message) : IMessageLocalizer {
        public ValueTask<string?> LocalizeAsync(
            string key,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<string?>(message);

        public ValueTask<string?> LocalizeAsync(
            string key,
            IReadOnlyDictionary<string, object?> parameters,
            CultureInfo culture,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<string?>(message);

        public ValueTask<string?> LocalizePluralAsync(
            string singularKey,
            string pluralKey,
            decimal count,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<string?>(message);

        public ValueTask<string?> LocalizePluralAsync(
            string singularKey,
            string pluralKey,
            decimal count,
            IReadOnlyDictionary<string, object?> parameters,
            CultureInfo culture,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<string?>(message);

        public ValueTask<string?> LocalizeWithContextAsync(
            string key,
            string context,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<string?>(message);

        public ValueTask<string?> LocalizeWithContextAsync(
            string key,
            string context,
            IReadOnlyDictionary<string, object?> parameters,
            CultureInfo culture,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<string?>(message);
    }
}
