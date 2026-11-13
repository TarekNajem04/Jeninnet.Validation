//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Describes one validation call passed to an <see cref="IValidationInterceptor"/>.
/// </summary>
public sealed class ValidationInterceptorContext {
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationInterceptorContext"/> class.
    /// </summary>
    /// <param name="instance">The model instance.</param>
    /// <param name="modelType">The model type.</param>
    /// <param name="validatorName">The concrete validator type name.</param>
    /// <param name="serviceProvider">The request service provider.</param>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="endpoint">The minimal API endpoint, when available.</param>
    /// <param name="actionDescriptor">The MVC action descriptor, when available.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    public ValidationInterceptorContext(
        object instance,
        Type modelType,
        string? validatorName,
        IServiceProvider serviceProvider,
        HttpContext httpContext,
        Endpoint? endpoint = null,
        ActionDescriptor? actionDescriptor = null) {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(httpContext);

        Instance = instance;
        ModelType = modelType;
        ValidatorName = validatorName;
        ServiceProvider = serviceProvider;
        HttpContext = httpContext;
        Endpoint = endpoint;
        ActionDescriptor = actionDescriptor;
    }

    /// <summary>Gets the model instance being validated.</summary>
    public object Instance { get; }

    /// <summary>Gets the model type.</summary>
    public Type ModelType { get; }

    /// <summary>Gets the concrete validator type name, when known.</summary>
    public string? ValidatorName { get; }

    /// <summary>Gets the request service provider.</summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>Gets the HTTP context.</summary>
    public HttpContext HttpContext { get; }

    /// <summary>Gets the minimal API endpoint, when the call came from minimal APIs.</summary>
    public Endpoint? Endpoint { get; }

    /// <summary>Gets the MVC action descriptor, when the call came from MVC.</summary>
    public ActionDescriptor? ActionDescriptor { get; }
}
