//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Assertions;

/// <summary>
/// Builds assertion failure messages with the actual validation errors attached.
/// </summary>
internal static class AssertionMessages {
    internal static ValidationAssertionException Failure(string expectation, IReadOnlyList<ValidationError> actualErrors) {
        var details = actualErrors.Count == 0
            ? "none"
            : string.Join("; ", actualErrors.Select(static error => $"{error.MemberName}: {Describe(error)}"));

        return new ValidationAssertionException($"{expectation}. Actual errors: {details}.");
    }

    private static string Describe(ValidationError error) {
        var identifier = error.MessageKey ?? error.Message ?? error.MessageId.Id;
        var message = error.Message is null ? string.Empty : $" message: '{error.Message}'";

        return $"'{identifier}'{message} (code: {error.ErrorCode ?? "none"}, severity: {error.Severity})";
    }
}
