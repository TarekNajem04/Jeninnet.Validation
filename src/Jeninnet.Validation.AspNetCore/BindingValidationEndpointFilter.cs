//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Validates every bound minimal API argument that has a registered <see cref="IValidator{T}"/>,
/// including body, query, route, and header parameters.
/// </summary>
/// <param name="method">The endpoint handler method used to discover binding metadata.</param>
public sealed class ValidationEndpointFilter(MethodInfo? method) : IEndpointFilter {
    private static readonly ConcurrentDictionary<Type, Type> _validatorInterfaces = new();

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next) {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var endpoint = context.HttpContext.GetEndpoint();

        if(ShouldSkip(endpoint, method)) {
            return await next(context).ConfigureAwait(false);
        }

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var parameters = method?.GetParameters() ?? [];
        var ruleSets = GetRuleSets(endpoint, method);
        var requestServices = context.HttpContext.RequestServices;
        var interceptors = ValidationInterceptorInvoker.Resolve(requestServices);

        for(var i = 0; i < context.Arguments.Count && i < parameters.Length; i++) {
            await ValidateArgumentAsync(
                context,
                parameters[i],
                context.Arguments[i],
                ruleSets,
                interceptors,
                errors).ConfigureAwait(false);
        }

        if(errors.Count > 0) {
            return CreateProblem(requestServices, errors);
        }

        return await next(context).ConfigureAwait(false);
    }

    private static async ValueTask ValidateArgumentAsync(
        EndpointFilterInvocationContext context,
        ParameterInfo parameter,
        object? argument,
        IEnumerable<string>? ruleSets,
        IReadOnlyList<IValidationInterceptor>? interceptors,
        Dictionary<string, List<string>> errors) {
        if(argument is null || ShouldSkipParameter(parameter)) {
            return;
        }

        var modelType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        var requestServices = context.HttpContext.RequestServices;
        var validator = ResolveValidator(modelType, requestServices);

        if(validator is null) {
            return;
        }

        var result = await InvokeValidatorAsync(
            validator,
            argument,
            modelType,
            ruleSets,
            interceptors,
            context).ConfigureAwait(false);

        if(result.IsValid) {
            return;
        }

        var localizedErrors = await result.Errors
            .ToProblemErrorsAsync(
                requestServices.GetService<ValidationProblemOptions>(),
                requestServices.GetService<IMessageLocalizer>(),
                CultureInfo.CurrentUICulture,
                context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        MergeErrors(errors, localizedErrors, parameter);
    }

    private static object? ResolveValidator(Type modelType, IServiceProvider requestServices) {
        var validatorInterface = _validatorInterfaces.GetOrAdd(
            modelType, static type => typeof(IValidator<>).MakeGenericType(type));

        return requestServices.GetService(validatorInterface)
            ?? ValidationConfiguration.CreateValidator(modelType, requestServices);
    }

    private static async ValueTask<ValidationResult> InvokeValidatorAsync(
        object validator,
        object argument,
        Type modelType,
        IEnumerable<string>? ruleSets,
        IReadOnlyList<IValidationInterceptor>? interceptors,
        EndpointFilterInvocationContext context) {
        var requestServices = context.HttpContext.RequestServices;
        var cancellationToken = context.HttpContext.RequestAborted;
        var invoker = ValidationInvoker.Invokers.GetOrAdd(
            modelType, static type => ValidationInvoker.Create(type));

        if(interceptors is null) {
            return await invoker(validator, argument, ruleSets, requestServices, cancellationToken)
                .ConfigureAwait(false);
        }

        var interceptorContext = new ValidationInterceptorContext(
            argument,
            modelType,
            validator.GetType().FullName,
            requestServices,
            context.HttpContext,
            context.HttpContext.GetEndpoint());

        return await ValidationInterceptorInvoker.InvokeAsync(
            interceptors,
            interceptorContext,
            token => invoker(validator, argument, ruleSets, requestServices, token),
            cancellationToken).ConfigureAwait(false);
    }

    private static void MergeErrors(
        Dictionary<string, List<string>> errors,
        IDictionary<string, string[]> localizedErrors,
        ParameterInfo parameter) {
        var prefix = GetBindingPrefix(parameter);

        foreach(var pair in localizedErrors) {
            var memberKey = string.IsNullOrEmpty(prefix) || pair.Key is not ("Value" or "")
                ? pair.Key
                : parameter.Name ?? pair.Key;
            var key = string.IsNullOrEmpty(prefix) ? pair.Key : $"{prefix}.{memberKey}";

            if(!errors.TryGetValue(key, out var messages)) {
                messages = [];
                errors[key] = messages;
            }

            messages.AddRange(pair.Value);
        }
    }

    private static IResult CreateProblem(
        IServiceProvider requestServices,
        Dictionary<string, List<string>> errors) {
        var options = requestServices.GetService<ValidationProblemOptions>();

        return Results.ValidationProblem(
            errors.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray(), StringComparer.Ordinal),
            statusCode: StatusCodes.Status400BadRequest,
            title: options?.Title);
    }

    private static bool ShouldSkip(Endpoint? endpoint, MethodInfo? method) {
        if(endpoint?.Metadata.OfType<SkipJeninnetValidationAttribute>().Any() == true ||
           endpoint?.Metadata.OfType<DisableValidationAttribute>().Any() == true) {
            return true;
        }

        return method?.GetCustomAttribute<SkipJeninnetValidationAttribute>(inherit: true) is not null ||
               method?.GetCustomAttribute<DisableValidationAttribute>(inherit: true) is not null;
    }

    private static bool ShouldSkipParameter(ParameterInfo parameter) =>
        parameter.GetCustomAttribute<SkipJeninnetValidationAttribute>(inherit: true) is not null ||
        parameter.GetCustomAttribute<DisableValidationAttribute>(inherit: true) is not null ||
        parameter.GetCustomAttribute<SkipValidationAttribute>(inherit: true) is not null;

    private static string? GetBindingPrefix(ParameterInfo parameter) {
        if(parameter.GetCustomAttribute<FromQueryAttribute>(inherit: true) is not null) {
            return "query";
        }

        if(parameter.GetCustomAttribute<FromRouteAttribute>(inherit: true) is not null) {
            return "route";
        }

        if(parameter.GetCustomAttribute<FromHeaderAttribute>(inherit: true) is not null) {
            return "header";
        }

        return null;
    }

    private static IEnumerable<string>? GetRuleSets(Endpoint? endpoint, MethodInfo? method) {
        var endpointRuleSets = endpoint?.Metadata.OfType<JeninnetValidationRuleSetsAttribute>().FirstOrDefault();

        if(endpointRuleSets is not null) {
            return endpointRuleSets.RuleSets;
        }

        return method?.GetCustomAttribute<JeninnetValidationRuleSetsAttribute>(inherit: true)?.RuleSets;
    }
}
