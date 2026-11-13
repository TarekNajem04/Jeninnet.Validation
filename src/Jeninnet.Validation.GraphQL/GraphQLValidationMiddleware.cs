//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.GraphQL;

/// <summary>
/// HotChocolate field middleware that validates input-object arguments before the resolver runs.
/// Invalid input adds GraphQL errors with <c>extensions.member</c> and <c>extensions.code</c> and
/// skips the resolver.
/// </summary>
public sealed class GraphQLValidationMiddleware {
    private readonly FieldDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLValidationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next field middleware.</param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is null.</exception>
    public GraphQLValidationMiddleware(FieldDelegate next) {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    /// <summary>
    /// Validates the field's input-object arguments and invokes the next middleware.
    /// </summary>
    /// <param name="context">The middleware context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public async ValueTask InvokeAsync(IMiddlewareContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var services = context.RequestServices;
        var options = services.GetService<GraphQLValidationOptions>() ?? new GraphQLValidationOptions();
        var collector = new ValidationErrorCollector();
        var hasErrors = await ValidateArgumentsAsync(context, services, options, collector).ConfigureAwait(false);

        if(!hasErrors) {
            await _next(context).ConfigureAwait(false);
            return;
        }

        ReportErrors(context, collector);
        context.Result = null;
    }

    private static async ValueTask<bool> ValidateArgumentsAsync(
        IMiddlewareContext context,
        IServiceProvider services,
        GraphQLValidationOptions options,
        ValidationErrorCollector collector) {
        var hasErrors = false;

        foreach(var argument in context.Selection.Field.Arguments) {
            var optional = context.ArgumentOptional<object?>(argument.Name);

            if(!optional.HasValue || optional.Value is null) {
                continue;
            }

            hasErrors |= await ValidateArgumentAsync(context, services, options, collector, optional.Value)
                .ConfigureAwait(false);
        }

        return hasErrors;
    }

    private static async ValueTask<bool> ValidateArgumentAsync(
        IMiddlewareContext context,
        IServiceProvider services,
        GraphQLValidationOptions options,
        ValidationErrorCollector collector,
        object value) {
        var modelType = value.GetType();
        var validatorInterface = typeof(IValidator<>).MakeGenericType(modelType);
        var validators = services.GetServices(validatorInterface).Where(static validator => validator is not null).ToList();

        if(validators.Count == 0) {
            return false;
        }

        var ruleSets = GraphQLValidationRuleSetsCache.Get(modelType);

        if(ruleSets.Count == 0) {
            ruleSets = [.. options.RuleSets];
        }

        var hasErrors = false;

        foreach(var validator in validators) {
            var result = await InvokeValidatorAsync(
                validator!,
                value,
                modelType,
                ruleSets,
                context.RequestAborted).ConfigureAwait(false);

            if(!result.IsValid) {
                collector.AddRange(result.Errors);
                hasErrors = true;
            }
        }

        return hasErrors;
    }

    private static void ReportErrors(IMiddlewareContext context, ValidationErrorCollector collector) {
        foreach(var error in collector) {
            var builder = ErrorBuilder.New()
                .SetMessage(GetMessage(error))
                .SetCode(GetCode(error))
                .SetExtension("severity", error.Severity.ToString());

            if(!string.IsNullOrWhiteSpace(error.MemberName)) {
                builder.SetExtension("member", error.MemberName);
            }

            if(!string.IsNullOrWhiteSpace(error.ErrorCode)) {
                builder.SetExtension("errorCode", error.ErrorCode);
            }

            context.ReportError(builder.Build());
        }
    }

    private static async ValueTask<ValidationResult> InvokeValidatorAsync(
        object validator,
        object value,
        Type modelType,
        IReadOnlyList<string> ruleSets,
        CancellationToken cancellationToken) {
        var optionsInterface = typeof(IValidationOptionsValidator<>).MakeGenericType(modelType);

        if(optionsInterface.IsInstanceOfType(validator)) {
            var method = optionsInterface.GetMethod(
                "ValidateAsync",
                [modelType, typeof(ValidationCallOptions), typeof(CancellationToken)])
                ?? throw new InvalidOperationException(
                    $"Could not find the property-level ValidateAsync method for IValidator<{modelType.Name}>.");

            var callOptions = new ValidationCallOptions {
                RuleSets = ruleSets.Count > 0 ? ruleSets : null
            };

            return await InvokeValueTaskAsync(method, validator, [value, callOptions, cancellationToken])
                .ConfigureAwait(false);
        }

        if(ruleSets.Count > 0) {
            throw new InvalidOperationException(
                $"Validator '{validator.GetType().FullName}' does not implement " +
                $"IValidationOptionsValidator<{modelType.Name}>; rule-set selection is not supported.");
        }

        var asyncInterface = typeof(IAsyncValidator<>).MakeGenericType(modelType);
        var asyncMethod = asyncInterface.GetMethod(
            "ValidateAsync",
            [modelType, typeof(CancellationToken)])
            ?? throw new InvalidOperationException(
                $"Could not find the asynchronous ValidateAsync method for IValidator<{modelType.Name}>.");

        return await InvokeValueTaskAsync(asyncMethod, validator, [value, cancellationToken])
            .ConfigureAwait(false);
    }

    private static async ValueTask<ValidationResult> InvokeValueTaskAsync(
        MethodInfo method,
        object validator,
        object?[] arguments) {
        try {
            var result = method.Invoke(validator, arguments);

            if(result is ValueTask<ValidationResult> valueTask) {
                return await valueTask.ConfigureAwait(false);
            }

            throw new InvalidOperationException(
                $"Validator '{validator.GetType().FullName}' returned no validation result.");
        }
        catch(TargetInvocationException exception) when(exception.InnerException is not null) {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static string GetCode(ValidationError error) {
        if(!string.IsNullOrWhiteSpace(error.ErrorCode)) {
            return error.ErrorCode;
        }

        if(!string.IsNullOrWhiteSpace(error.MessageKey)) {
            return error.MessageKey;
        }

        if(!string.IsNullOrWhiteSpace(error.MessageId.Id)) {
            return error.MessageId.Id;
        }

        return "Validation";
    }

    private static string GetMessage(ValidationError error) {
        var message = error.Message;

        if(string.IsNullOrWhiteSpace(message)) {
            message = error.MessageKey;
        }

        if(string.IsNullOrWhiteSpace(message)) {
            message = error.MessageId.Id;
        }

        return string.IsNullOrWhiteSpace(message) ? "Invalid" : message;
    }
}

internal static class GraphQLValidationRuleSetsCache {
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<string>> _cache = new();

    internal static IReadOnlyList<string> Get(Type inputType) =>
        _cache.GetOrAdd(
            inputType,
            static type => type.GetCustomAttribute<GraphQLValidationRuleSetsAttribute>(inherit: true)?.RuleSets ?? []);
}
