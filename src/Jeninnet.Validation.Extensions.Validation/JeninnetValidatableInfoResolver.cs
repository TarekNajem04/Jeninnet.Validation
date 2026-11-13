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
/// Resolves validation metadata for explicitly registered Jeninnet models.
/// </summary>
internal sealed class JeninnetValidatableInfoResolver : IValidatableInfoResolver {
    private readonly IReadOnlyDictionary<Type, Func<ValidatableTypeInfo>> _factories;

    internal JeninnetValidatableInfoResolver(IReadOnlyDictionary<Type, Func<ValidatableTypeInfo>> factories) {
        ArgumentNullException.ThrowIfNull(factories);
        _factories = factories;
    }

#if NET11_0_OR_GREATER
    /// <inheritdoc />
    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo? validatableTypeInfo) =>
        TryResolve(type, out validatableTypeInfo);

    /// <inheritdoc />
    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo? validatableParameterInfo) {
        ArgumentNullException.ThrowIfNull(parameterInfo);

        if(!HasDataAnnotationsBridge(parameterInfo.ParameterType) &&
           _factories.TryGetValue(parameterInfo.ParameterType, out var factory) &&
           factory() is IValidatableParameterInfo parameterAdapter) {
            validatableParameterInfo = parameterAdapter;
            return true;
        }

        validatableParameterInfo = null;
        return false;
    }
#else
    /// <inheritdoc />
    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableInfo) =>
        TryResolve(type, out validatableInfo);

    /// <inheritdoc />
    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? validatableInfo) {
        ArgumentNullException.ThrowIfNull(parameterInfo);
        validatableInfo = null;
        return false;
    }
#endif

    internal static bool HasDataAnnotationsBridge(Type type) =>
        type.GetCustomAttributes(typeof(JeninnetValidatableAttribute), inherit: true).Length > 0;

    private bool TryResolve(Type type, [NotNullWhen(true)] out ValidatableTypeInfo? validatableInfo) {
        ArgumentNullException.ThrowIfNull(type);

        if(!HasDataAnnotationsBridge(type) && _factories.TryGetValue(type, out var factory)) {
            validatableInfo = factory();
            return true;
        }

        validatableInfo = null;
        return false;
    }
}
