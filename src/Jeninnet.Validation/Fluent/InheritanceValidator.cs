//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Maps concrete runtime types to validators for a shared base type or interface.
/// </summary>
/// <typeparam name="TBase">The base type or interface being validated.</typeparam>
/// <remarks>
/// The map is an explicit dispatch table. Only the exact runtime type is resolved; a type that
/// is not registered is skipped. Register each concrete type instead of relying on reflection or
/// automatic discovery.
/// </remarks>
public sealed class InheritanceValidatorMap<TBase> where TBase : class {
    private readonly Dictionary<Type, IInheritanceValidatorEntry<TBase>> _entries = [];

    /// <summary>
    /// Registers a combined synchronous/asynchronous validator for <typeparamref name="TDerived"/>.
    /// </summary>
    /// <typeparam name="TDerived">The concrete type handled by the validator.</typeparam>
    /// <param name="validator">The validator for the concrete type.</param>
    /// <returns>The same map for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> is null.</exception>
    /// <exception cref="ArgumentException">A validator is already registered for the type.</exception>
    public InheritanceValidatorMap<TBase> Add<TDerived>(IValidator<TDerived> validator)
        where TDerived : class, TBase {
        ArgumentNullException.ThrowIfNull(validator);

        return AddCore<TDerived>(new InheritanceValidatorEntry<TBase, TDerived>(validator, validator, validator));
    }

    /// <summary>
    /// Registers a synchronous validator for <typeparamref name="TDerived"/>.
    /// </summary>
    /// <typeparam name="TDerived">The concrete type handled by the validator.</typeparam>
    /// <param name="validator">The synchronous validator for the concrete type.</param>
    /// <returns>The same map for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> is null.</exception>
    /// <exception cref="ArgumentException">A validator is already registered for the type.</exception>
    public InheritanceValidatorMap<TBase> Add<TDerived>(ISyncValidator<TDerived> validator)
        where TDerived : class, TBase {
        ArgumentNullException.ThrowIfNull(validator);

        return AddCore<TDerived>(new InheritanceValidatorEntry<TBase, TDerived>(null, validator, null));
    }

    /// <summary>
    /// Registers an asynchronous validator for <typeparamref name="TDerived"/>.
    /// </summary>
    /// <typeparam name="TDerived">The concrete type handled by the validator.</typeparam>
    /// <param name="validator">The asynchronous validator for the concrete type.</param>
    /// <returns>The same map for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> is null.</exception>
    /// <exception cref="ArgumentException">A validator is already registered for the type.</exception>
    public InheritanceValidatorMap<TBase> Add<TDerived>(IAsyncValidator<TDerived> validator)
        where TDerived : class, TBase {
        ArgumentNullException.ThrowIfNull(validator);

        return AddCore<TDerived>(new InheritanceValidatorEntry<TBase, TDerived>(null, null, validator));
    }

    internal bool TryResolve(Type runtimeType, out IInheritanceValidatorEntry<TBase> entry) =>
        _entries.TryGetValue(runtimeType, out entry!);

    private InheritanceValidatorMap<TBase> AddCore<TDerived>(IInheritanceValidatorEntry<TBase> entry)
        where TDerived : class, TBase {
        if(!_entries.TryAdd(typeof(TDerived), entry)) {
            throw new ArgumentException(
                $"A validator for '{typeof(TDerived).FullName}' is already registered.",
                nameof(entry));
        }

        return this;
    }
}

/// <summary>
/// Dispatches validation to a validator selected by the actual runtime type of the instance.
/// </summary>
/// <typeparam name="TBase">The base type or interface being validated.</typeparam>
/// <remarks>
/// The dispatcher uses an explicit <see cref="InheritanceValidatorMap{TBase}"/>; it performs a
/// dictionary lookup on <see cref="object.GetType"/> and never uses reflection to construct or
/// invoke validators. Unknown runtime types are skipped.
/// </remarks>
public sealed class InheritanceValidator<TBase> : IValidator<TBase>, IValidationContextValidator<TBase>
    where TBase : class {
    private readonly InheritanceValidatorMap<TBase> _map;

    /// <summary>
    /// Initializes a new instance of the <see cref="InheritanceValidator{TBase}"/> class.
    /// </summary>
    /// <param name="map">The explicit runtime-type map.</param>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
    public InheritanceValidator(InheritanceValidatorMap<TBase> map) {
        ArgumentNullException.ThrowIfNull(map);
        _map = map;
    }

    /// <inheritdoc />
    public ValidationResult Validate(TBase instance) {
        ArgumentNullException.ThrowIfNull(instance);

        return _map.TryResolve(instance.GetType(), out var entry)
            ? entry.Validate(instance)
            : ValidationResult.Success;
    }

    /// <inheritdoc />
    public ValidationResult Validate(TBase instance, IEnumerable<string> ruleSets) {
        ArgumentNullException.ThrowIfNull(instance);

        return _map.TryResolve(instance.GetType(), out var entry)
            ? entry.Validate(instance, ruleSets)
            : ValidationResult.Success;
    }

    /// <inheritdoc />
    public ValidationResult Validate(TBase instance, ValidationContext<TBase> context) {
        ArgumentNullException.ThrowIfNull(instance);

        return _map.TryResolve(instance.GetType(), out var entry)
            ? entry.Validate(instance, context)
            : ValidationResult.Success;
    }

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(TBase instance, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(instance);

        return _map.TryResolve(instance.GetType(), out var entry)
            ? entry.ValidateAsync(instance, cancellationToken)
            : new ValueTask<ValidationResult>(ValidationResult.Success);
    }

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        TBase instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(instance);

        return _map.TryResolve(instance.GetType(), out var entry)
            ? entry.ValidateAsync(instance, ruleSets, cancellationToken)
            : new ValueTask<ValidationResult>(ValidationResult.Success);
    }

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        TBase instance,
        ValidationContext<TBase> context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(instance);

        return _map.TryResolve(instance.GetType(), out var entry)
            ? entry.ValidateAsync(instance, context, cancellationToken)
            : new ValueTask<ValidationResult>(ValidationResult.Success);
    }
}

internal interface IInheritanceValidatorEntry<TBase> where TBase : class {
    ValidationResult Validate(TBase instance);

    ValidationResult Validate(TBase instance, IEnumerable<string> ruleSets);

    ValidationResult Validate(TBase instance, ValidationContext<TBase> context);

    ValueTask<ValidationResult> ValidateAsync(TBase instance, CancellationToken cancellationToken);

    ValueTask<ValidationResult> ValidateAsync(
        TBase instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken);

    ValueTask<ValidationResult> ValidateAsync(
        TBase instance,
        ValidationContext<TBase> context,
        CancellationToken cancellationToken);
}

internal sealed class InheritanceValidatorEntry<TBase, TDerived>(
    IValidator<TDerived>? validator,
    ISyncValidator<TDerived>? syncValidator,
    IAsyncValidator<TDerived>? asyncValidator
) : IInheritanceValidatorEntry<TBase>
    where TBase : class
    where TDerived : class, TBase {
    private readonly IValidator<TDerived>? _validator = validator;
    private readonly ISyncValidator<TDerived>? _syncValidator = syncValidator;
    private readonly IAsyncValidator<TDerived>? _asyncValidator = asyncValidator;

    public ValidationResult Validate(TBase instance) {
        var typed = (TDerived)instance;

        if(_validator is not null) {
            return _validator.Validate(typed);
        }

        if(_syncValidator is not null) {
            return _syncValidator.Validate(typed);
        }

        throw AsyncOnly<TDerived>();
    }

    public ValidationResult Validate(TBase instance, IEnumerable<string> ruleSets) {
        var typed = (TDerived)instance;

        if(_validator is not null) {
            return _validator.Validate(typed, ruleSets);
        }

        if(_syncValidator is not null) {
            return _syncValidator.Validate(typed, ruleSets);
        }

        throw AsyncOnly<TDerived>();
    }

    public ValidationResult Validate(TBase instance, ValidationContext<TBase> context) {
        var typed = (TDerived)instance;

        if(_validator is IValidationContextValidator<TDerived> contextValidator) {
            return contextValidator.Validate(typed, Convert(context, typed));
        }

        return Prefix(Validate(instance), context.MemberPath);
    }

    public ValueTask<ValidationResult> ValidateAsync(TBase instance, CancellationToken cancellationToken) {
        var typed = (TDerived)instance;

        if(_asyncValidator is not null) {
            return _asyncValidator.ValidateAsync(typed, cancellationToken);
        }

        if(_syncValidator is not null) {
            return new ValueTask<ValidationResult>(_syncValidator.Validate(typed));
        }

        return new ValueTask<ValidationResult>(ValidationResult.Success);
    }

    public ValueTask<ValidationResult> ValidateAsync(
        TBase instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken) {
        var typed = (TDerived)instance;

        if(_asyncValidator is not null) {
            return _asyncValidator.ValidateAsync(typed, ruleSets, cancellationToken);
        }

        if(_syncValidator is not null) {
            return new ValueTask<ValidationResult>(_syncValidator.Validate(typed, ruleSets));
        }

        return new ValueTask<ValidationResult>(ValidationResult.Success);
    }

    public async ValueTask<ValidationResult> ValidateAsync(
        TBase instance,
        ValidationContext<TBase> context,
        CancellationToken cancellationToken) {
        var typed = (TDerived)instance;

        if(_asyncValidator is IValidationContextValidator<TDerived> contextAsyncValidator) {
            return await contextAsyncValidator.ValidateAsync(typed, Convert(context, typed), cancellationToken)
                                              .ConfigureAwait(false);
        }

        if(_asyncValidator is not null) {
            var asyncResult = await _asyncValidator.ValidateAsync(typed, cancellationToken).ConfigureAwait(false);
            return Prefix(asyncResult, context.MemberPath);
        }

        if(_validator is IValidationContextValidator<TDerived> contextSyncValidator) {
            return await contextSyncValidator.ValidateAsync(typed, Convert(context, typed), cancellationToken)
                                             .ConfigureAwait(false);
        }

        if(_syncValidator is not null) {
            return Prefix(_syncValidator.Validate(typed), context.MemberPath);
        }

        return ValidationResult.Success;
    }

    private static ValidationContext<TDerived> Convert(ValidationContext<TBase> context, TDerived instance) =>
        new(
            instance,
            context.MemberPath,
            context.RuleSets,
            context.FailFast,
            context.Root,
            context.RootData,
            context.ServiceProvider);

    private static ValidationResult Prefix(ValidationResult result, string memberPath) {
        if(result.IsValid || string.IsNullOrEmpty(memberPath)) {
            return result;
        }

        var errors = new ValidationErrorCollector();
        ValidationErrorPath.AddRange(errors, result, memberPath);
        return ValidationResult.FromErrors(errors);
    }

    private static InvalidOperationException AsyncOnly<T>() =>
        new($"The inheritance validator for '{typeof(T).FullName}' is asynchronous-only; call ValidateAsync instead.");
}
