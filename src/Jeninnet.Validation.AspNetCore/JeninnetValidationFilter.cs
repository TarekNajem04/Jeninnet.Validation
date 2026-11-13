//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Validates MVC action arguments with the registered <see cref="IValidator{T}"/> and feeds
/// <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary"/>.
/// </summary>
/// <param name="options">The MVC validation options.</param>
/// <param name="interceptors">Optional validation interceptors, run in registration order.</param>
public sealed class JeninnetValidationFilter(
    MvcValidationOptions options,
    IEnumerable<IValidationInterceptor> interceptors) : IAsyncActionFilter {
    private static readonly ConcurrentDictionary<Type, Type> _validatorInterfaces = new();
    private readonly IReadOnlyList<IValidationInterceptor>? _interceptors =
        interceptors is null ? null : [.. interceptors];

    /// <summary>
    /// Initializes a new instance of the <see cref="JeninnetValidationFilter"/> class without
    /// interceptors.
    /// </summary>
    /// <param name="options">The MVC validation options.</param>
    public JeninnetValidationFilter(MvcValidationOptions options)
        : this(options, []) {
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if(ShouldSkip(context.ActionDescriptor)) {
            await next().ConfigureAwait(false);
            return;
        }

        var ruleSets = GetRuleSets(context.ActionDescriptor);
        var requestServices = context.HttpContext.RequestServices;
        var hasErrors = false;
        var started = Stopwatch.GetTimestamp();
        string? validatedModelType = null;
        var skippedArguments = GetSkippedArgumentNames(context.ActionDescriptor);

        foreach(var argument in context.ActionArguments) {
            if(argument.Value is null || skippedArguments?.Contains(argument.Key) == true) {
                continue;
            }

            var model = argument.Value;
            var modelType = model.GetType();
            var validatorInterface = _validatorInterfaces.GetOrAdd(
                modelType, static type => typeof(IValidator<>).MakeGenericType(type));
            var validator = requestServices.GetService(validatorInterface)
                ?? ValidationConfiguration.CreateValidator(modelType, requestServices);

            if(validator is null) {
                continue;
            }

            validatedModelType ??= modelType.FullName ?? modelType.Name;
            var invoker = ValidationInvoker.Invokers.GetOrAdd(modelType, static type => ValidationInvoker.Create(type));
            var interceptorContext = new ValidationInterceptorContext(
                model,
                modelType,
                validator.GetType().FullName,
                requestServices,
                context.HttpContext,
                context.HttpContext.GetEndpoint(),
                context.ActionDescriptor);
            var result = _interceptors is null || _interceptors.Count is 0
                ? await invoker(validator, model, ruleSets, requestServices, context.HttpContext.RequestAborted)
                    .ConfigureAwait(false)
                : await ValidationInterceptorInvoker.InvokeAsync(
                    _interceptors,
                    interceptorContext,
                    cancellationToken => invoker(validator, model, ruleSets, requestServices, cancellationToken),
                    context.HttpContext.RequestAborted).ConfigureAwait(false);

            if(result.IsValid) {
                continue;
            }

            hasErrors = true;
            var localizer = context.HttpContext.RequestServices.GetService<IMessageLocalizer>();

            foreach(var error in result.Errors) {
                var key = options.ModelStateKeySelector?.Invoke(error) ?? error.MemberName;
                context.ModelState.AddModelError(
                    key,
                    await FormatAsync(error, localizer, context.HttpContext.RequestAborted).ConfigureAwait(false));
            }
        }

        if(hasErrors) {
            var elapsed = Stopwatch.GetElapsedTime(started);
            var problem = new ValidationProblemDetails(context.ModelState);
            ValidationProblemExtensions.EnrichProblem(
                problem,
                context.HttpContext,
                options.ProblemOptions,
                validatedModelType,
                elapsed);

            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next().ConfigureAwait(false);
    }

    private static HashSet<string>? GetSkippedArgumentNames(ActionDescriptor descriptor) {
        if(descriptor is not ControllerActionDescriptor controller) {
            return null;
        }

        HashSet<string>? names = null;

        foreach(var parameter in controller.Parameters) {
            if(parameter is ControllerParameterDescriptor { ParameterInfo: { } parameterInfo } &&
               parameterInfo.GetCustomAttribute<SkipValidationAttribute>(inherit: true) is not null) {
                (names ??= [with(StringComparer.Ordinal)]).Add(parameter.Name);
            }
        }

        return names;
    }

    private static bool ShouldSkip(ActionDescriptor descriptor) {
        if(descriptor.EndpointMetadata.OfType<SkipJeninnetValidationAttribute>().Any()) {
            return true;
        }

        if(descriptor is not ControllerActionDescriptor controller) {
            return false;
        }

        return controller.MethodInfo?.GetCustomAttribute<SkipJeninnetValidationAttribute>(inherit: true) is not null ||
               controller.ControllerTypeInfo?.GetCustomAttribute<SkipJeninnetValidationAttribute>(inherit: true) is not null;
    }

    private static IEnumerable<string>? GetRuleSets(ActionDescriptor descriptor) {
        var metadata = descriptor.EndpointMetadata.OfType<JeninnetValidationRuleSetsAttribute>().FirstOrDefault();

        if(metadata is not null) {
            return metadata.RuleSets;
        }

        if(descriptor is not ControllerActionDescriptor controller) {
            return null;
        }

        var method = controller.MethodInfo.GetCustomAttribute<JeninnetValidationRuleSetsAttribute>(inherit: true);
        if(method is not null) {
            return method.RuleSets;
        }

        return controller.ControllerTypeInfo
            .GetCustomAttribute<JeninnetValidationRuleSetsAttribute>(inherit: true)?
            .RuleSets;
    }

    private async ValueTask<string> FormatAsync(
        ValidationError error,
        IMessageLocalizer? localizer,
        CancellationToken cancellationToken) {
        var message = error.Message;

        if(message is null &&
           !error.MessageIsAuthored &&
           localizer is not null &&
           error.MessageKey is not null) {
            var parameters = ValidationProblemExtensions.ToLocalizationParameters(error.MessageParameters);

            message = await localizer
                .LocalizeAsync(error.MessageKey, parameters, CultureInfo.CurrentUICulture, cancellationToken)
                .ConfigureAwait(false);
        }

        message ??= error.MessageKey ?? error.ErrorCode ?? "Invalid";

        if(options.IncludeErrorCodes && error.ErrorCode is not null &&
           !string.Equals(error.ErrorCode, message, StringComparison.Ordinal)) {
            message = $"{message} [{error.ErrorCode}]";
        }

        if(options.IncludeSeverities) {
            message = $"{message} ({error.Severity})";
        }

        return message;
    }
}
