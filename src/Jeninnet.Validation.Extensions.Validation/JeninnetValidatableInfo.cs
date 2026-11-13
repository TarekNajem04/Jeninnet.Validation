//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

#if NET11_0_OR_GREATER
using ValidatableTypeInfo = Microsoft.Extensions.Validation.IValidatableTypeInfo;
#else
using ValidatableTypeInfo = Microsoft.Extensions.Validation.IValidatableInfo;
#endif

namespace Jeninnet.Validation.Extensions.Validation;

/// <summary>
/// Adapts a Jeninnet validator to the <c>Microsoft.Extensions.Validation</c> pipeline.
/// </summary>
/// <typeparam name="TModel">The validated model type.</typeparam>
#if NET11_0_OR_GREATER
internal sealed class JeninnetValidatableInfo<TModel> : ValidatableTypeInfo, Microsoft.Extensions.Validation.IValidatableParameterInfo {
#else
internal sealed class JeninnetValidatableInfo<TModel> : ValidatableTypeInfo {
#endif
#if NET11_0_OR_GREATER
    /// <inheritdoc />
    public void Validate(object? value, ValidateContext context) {
        ArgumentNullException.ThrowIfNull(context);

        if(value is not TModel model) {
            return;
        }

        if(context.ServiceProvider?.GetService(typeof(IValidator<TModel>)) is not IValidator<TModel> validator) {
            return;
        }

        var result = validator.Validate(model);

        if(!result.IsValid) {
            AddErrors(result, context, value);
        }
    }

    /// <inheritdoc />
    public bool TryFindProperty(
        string propertyName,
        ValidationOptions validationOptions,
        [NotNullWhen(true)] out IValidatablePropertyInfo? validatablePropertyInfo) {
        validatablePropertyInfo = null;
        return false;
    }
#endif

    /// <inheritdoc />
    public async Task ValidateAsync(
        object? value,
        ValidateContext context,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(context);

        if(value is not TModel model) {
            return;
        }

#if NET11_0_OR_GREATER
        var serviceProvider = context.ServiceProvider;
#else
        var serviceProvider = context.ValidationContext;
#endif

        if(serviceProvider?.GetService(typeof(IValidator<TModel>)) is not IValidator<TModel> validator) {
            return;
        }

        var result = await validator.ValidateAsync(model, cancellationToken).ConfigureAwait(false);

        if(result.IsValid) {
            return;
        }

        AddErrors(result, context, value);
    }

    private static void AddErrors(
        JeninnetValidationResult result,
        ValidateContext context,
        object? value) {
#if NET11_0_OR_GREATER
        var prefix = context.CurrentValidationPath ?? string.Empty;

        foreach(var error in result.Errors) {
            var path = CombinePath(prefix, error.MemberName);
            var message = error.Message ?? error.MessageKey ?? error.ErrorCode ?? "Invalid";

            context.AddValidationError(new Microsoft.Extensions.Validation.ValidationError {
                Name = error.MemberName ?? string.Empty,
                Path = path,
                ErrorMessage = message,
                Container = value
            });
        }
#else
        _ = value;
        var errors = context.ValidationErrors ??= [with(StringComparer.Ordinal)];
        var prefix = context.CurrentValidationPath ?? string.Empty;

        foreach(var error in result.Errors) {
            var path = CombinePath(prefix, error.MemberName);
            var message = error.Message ?? error.MessageKey ?? error.ErrorCode ?? "Invalid";

            errors[path] = errors.TryGetValue(path, out var existing) ? [.. existing, message] : [message];
        }
#endif
    }

    private static string CombinePath(string prefix, string? memberName) {
        if(string.IsNullOrEmpty(prefix)) {
            return memberName ?? string.Empty;
        }

        if(string.IsNullOrEmpty(memberName)) {
            return prefix;
        }

        return $"{prefix}.{memberName}";
    }
}
