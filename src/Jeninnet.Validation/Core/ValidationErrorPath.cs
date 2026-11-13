//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Normalizes error member paths when a context-aware validator emits a relative path. Rules that
/// already use <see cref="ValidationContext{T}.GetFullPath"/> keep their path; custom rules that
/// hard-code a relative member name are prefixed with the inherited member path.
/// </summary>
internal static class ValidationErrorPath {
    internal static void AddRange(
        ValidationErrorCollector errors,
        ValidationResult result,
        string memberPath,
        RuleOptionOverrides? options = null,
        int? collectionIndex = null) =>
        AddRangeCore(
            errors,
            result,
            memberPath,
            options is null ? null : options.Apply,
            collectionIndex);

    internal static void AddRange<T>(
        ValidationErrorCollector errors,
        ValidationResult result,
        string memberPath,
        RuleOptionOverrides? options,
        int? collectionIndex,
        T instance,
        ValidationContext<T> context) =>
        AddRangeCore(
            errors,
            result,
            memberPath,
            options is null ? null : error => options.Apply(error, instance, context),
            collectionIndex);

    private static void AddRangeCore(
        ValidationErrorCollector errors,
        ValidationResult result,
        string memberPath,
        Func<ValidationError, ValidationError>? transform,
        int? collectionIndex) {
        for(var i = 0; i < result.Errors.Count; i++) {
            var error = PrepareError(result.Errors[i], memberPath, collectionIndex);

            errors.Add(transform is null ? error : transform(error));
        }
    }

    private static ValidationError PrepareError(
        ValidationError error,
        string memberPath,
        int? collectionIndex) {
        error = ApplyMemberPath(error, memberPath);

        if(collectionIndex is not null) {
            error = ApplyCollectionIndex(error, collectionIndex.Value);
        }

        if(error.MessageParameters?.ContainsKey(MessageParameterNames.PropertyPath) != true) {
            error = WithMemberPath(error, error.MemberName);
        }

        return error;
    }

    private static ValidationError ApplyMemberPath(ValidationError error, string memberPath) {
        if(string.IsNullOrEmpty(memberPath) || IsAlreadyPrefixed(error.MemberName, memberPath)) {
            return error;
        }

        var memberName = string.IsNullOrEmpty(error.MemberName)
            ? memberPath
            : $"{memberPath}.{error.MemberName}";

        return WithMemberPath(error, memberName);
    }

    private static ValidationError ApplyCollectionIndex(ValidationError error, int collectionIndex) {
        var parameters = error.MessageParameters is null
            ? []
            : new Dictionary<string, object>(error.MessageParameters, StringComparer.Ordinal);

        if(!parameters.ContainsKey(MessageParameterNames.CollectionIndex)) {
            parameters[MessageParameterNames.CollectionIndex] = collectionIndex;
        }

        return new ValidationError(
            memberName: error.MemberName,
            messageId: error.MessageId,
            messageKey: error.MessageKey,
            messageParameters: parameters,
            message: error.Message,
            errorCode: error.ErrorCode,
            severity: error.Severity,
            customState: error.CustomState) {
            MessageIsAuthored = error.MessageIsAuthored
        };
    }

    internal static ValidationError WithMemberPath(ValidationError error, string memberName) {
        var parameters = error.MessageParameters is null
            ? [with(StringComparer.Ordinal)]
            : new Dictionary<string, object>(error.MessageParameters, StringComparer.Ordinal);

        if(!parameters.ContainsKey(MessageParameterNames.PropertyName)) {
            parameters[MessageParameterNames.PropertyName] = memberName;
        }

        parameters[MessageParameterNames.PropertyPath] = memberName;

        return new ValidationError(
            memberName,
            error.MessageId,
            error.MessageKey,
            parameters,
            error.Message,
            error.ErrorCode,
            error.Severity,
            error.CustomState) {
            MessageIsAuthored = error.MessageIsAuthored
        };
    }

    private static bool IsAlreadyPrefixed(string memberName, string memberPath) =>
        memberName.Equals(memberPath, StringComparison.Ordinal) ||
        memberName.StartsWith($"{memberPath}.", StringComparison.Ordinal) ||
        memberName.StartsWith($"{memberPath}[", StringComparison.Ordinal);
}
