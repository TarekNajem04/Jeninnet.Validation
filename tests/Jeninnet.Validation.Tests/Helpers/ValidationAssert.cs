//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Helpers;

/// <summary>
/// Provides fluent assertion helpers specific to the Jeninnet.Validation model.
/// </summary>
internal static class ValidationAssert {
    // -------------------------------------------------------------------------
    // Result-level assertions
    // -------------------------------------------------------------------------

    /// <summary>
    /// Asserts that <paramref name="result"/> contains no errors.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="because">Optional explanation of the failure.</param>
    public static void IsValid(ValidationResult result, string? because = null) {
        var errors = string.Join(", ", result.Errors.Select(e => $"[{e.MemberName}: {e.Message}]"));
        MSTestAssertions.That(() => result.Errors.Should().BeEmpty(
            $"Expected a valid result{FormatBecause(because)}, " +
            $"but found {result.Errors.Count} error(s): {errors}"));
    }

    /// <summary>
    /// Asserts that <paramref name="result"/> is invalid and contains at least one error
    /// whose <see cref="ValidationError.MemberName"/> equals <paramref name="memberName"/>.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="memberName">The member name to check for an error.</param>
    /// <param name="because">Optional explanation of the failure.</param>
    public static void HasError(
        ValidationResult result,
        string memberName,
        string? because = null) {

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse(
            $"Expected an invalid result for member '{memberName}'{FormatBecause(because)}, " +
            "but the result was valid."));

        var hasMatch = result.Errors.Any(e =>
            string.Equals(e.MemberName, memberName, StringComparison.Ordinal));

        if(hasMatch) {
            return;
        }

        var actual = string.Join(", ", result.Errors.Select(e => $"'{e.MemberName}'"));
        MSTestAssertions.That(() => result.Errors.Should().Contain(
            e => string.Equals(e.MemberName, memberName, StringComparison.Ordinal),
            $"Expected an error on member '{memberName}'{FormatBecause(because)}, " +
            $"but only found errors on: {actual}"));
    }

    /// <summary>
    /// Asserts that <paramref name="result"/> is invalid, contains exactly one error whose
    /// member name equals <paramref name="memberName"/>, and that the error message contains
    /// <paramref name="messageFragment"/> (case-insensitive).
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="memberName">The member name to check for an error.</param>
    /// <param name="messageFragment">The error message fragment to search for.</param>
    /// <param name="because">Optional explanation of the failure.</param>
    public static void HasErrorWithMessage(
        ValidationResult result,
        string memberName,
        string messageFragment,
        string? because = null
    ) {
        HasError(result, memberName, because);

        var match = result.Errors.FirstOrDefault(e =>
            string.Equals(e.MemberName, memberName, StringComparison.Ordinal));

        MSTestAssertions.That(() => match.Should().NotBeNull($"No error found for member '{memberName}'"));

        if(match.MemberName is null) {
            return;
        }

        MSTestAssertions.That(() => match.Message.Should().Contain(messageFragment,
            $"Expected the error message for '{memberName}' to contain '{messageFragment}'" +
            $"{FormatBecause(because)}, but was: '{match.Message}'"));
    }

    /// <summary>
    /// Asserts that <paramref name="result"/> contains exactly <paramref name="count"/> errors.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="count">The expected number of errors.</param>
    /// <param name="because">Optional explanation of the failure.</param>
    public static void ErrorCount(ValidationResult result, int count, string? because = null) =>
        MSTestAssertions.That(() => result.Errors.Should().HaveCount(count,
            $"Expected exactly {count} error(s){FormatBecause(because)}, but found {result.Errors.Count}."));

    /// <summary>
    /// Asserts that no error on <paramref name="result"/> has the specified
    /// <paramref name="memberName"/>.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="memberName">The member name to check for no error.</param>
    /// <param name="because">Optional explanation of the failure.</param>
    public static void NoErrorFor(
        ValidationResult result,
        string memberName,
        string? because = null) {
        var offender = result.Errors.FirstOrDefault(e =>
            string.Equals(e.MemberName, memberName, StringComparison.Ordinal));

        var found = offender.MemberName is null ? "none" : $"'{offender.Message}'";
        MSTestAssertions.That(() => result.Errors.Should().NotContain(
            e => string.Equals(e.MemberName, memberName, StringComparison.Ordinal),
            $"Expected no error on member '{memberName}'{FormatBecause(because)}, " +
            $"but found: {found}"));
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Formats the 'because' string for error messages.
    /// </summary>
    /// <param name="because">Optional explanation string.</param>
    private static string FormatBecause(string? because) =>
        string.IsNullOrWhiteSpace(because) ? string.Empty : $" because {because}";
}

/// <summary>
/// Executes a single <see cref="ISyncValidationRule{T}"/> against an instance and returns
/// the collected <see cref="ValidationResult"/>.  Used to test rules in isolation without
/// spinning up a full <see cref="CompositeValidator{T}"/>.
/// </summary>
internal static class RuleRunner {
    /// <summary>
    /// Runs <paramref name="rule"/> against <paramref name="instance"/> and returns the
    /// result.  The member path is intentionally set to the empty string so that error
    /// paths produced by the rule are just the property name, not a dotted chain.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule to run.</param>
    /// <param name="instance">The instance to validate.</param>
    public static ValidationResult Run<T>(ISyncValidationRule<T> rule, T instance) {
        var collector = new ValidationErrorCollector();
        var context = new ValidationContext<T>(instance, MemberPath: string.Empty);

        rule.Validate(in context, collector);
        return ValidationResult.FromErrors(collector);
    }

    /// <summary>
    /// Runs <paramref name="rule"/> under a parent context whose member path is
    /// <paramref name="parentPath"/>.  Used to verify that nested rules build the full
    /// dotted path correctly.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule to run.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="parentPath">The parent member path.</param>
    public static ValidationResult RunWithPath<T>(
        ISyncValidationRule<T> rule,
        T instance,
        string parentPath) {
        var collector = new ValidationErrorCollector();
        var context = new ValidationContext<T>(instance, MemberPath: parentPath);
        rule.Validate(in context, collector);
        return ValidationResult.FromErrors(collector);
    }

    /// <summary>
    /// Runs <paramref name="rule"/> asynchronously and returns the result.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule to run.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="ct">The cancellation token.</param>
    public static async Task<ValidationResult> RunAsync<T>(
        IAsyncValidationRule<T> rule,
        T instance,
        CancellationToken ct = default) {
        var collector = new ValidationErrorCollector();
        var context = new ValidationContext<T>(instance, MemberPath: string.Empty);
        await rule.ValidateAsync(context, collector, ct).ConfigureAwait(false);
        return ValidationResult.FromErrors(collector);
    }
}
