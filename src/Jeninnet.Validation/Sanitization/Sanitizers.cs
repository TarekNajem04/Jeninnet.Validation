//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sanitization;

/// <summary>
/// A whole-model sanitization extension point. Implementations mutate the instance in place
/// before validation rules run; registration is explicit, so nothing is sanitized silently.
/// </summary>
/// <typeparam name="T">The model type.</typeparam>
public interface ISanitizer<in T> {
    /// <summary>
    /// Sanitizes the instance in place before validation rules run.
    /// </summary>
    /// <param name="instance">The model instance to sanitize.</param>
    void Sanitize(T instance);
}

/// <summary>
/// One preprocessing step executed before validation rules. Sanitization steps are stored
/// separately from validation rules, so they never appear in descriptors, JSON Schema, OpenAPI,
/// or client-rule payloads.
/// </summary>
/// <typeparam name="T">The model type.</typeparam>
public interface ISanitizationStep<in T> {
    /// <summary>
    /// Runs the preprocessing step against the instance.
    /// </summary>
    /// <param name="instance">The model instance.</param>
    void Sanitize(T instance);
}

/// <summary>
/// Creates sanitization pipeline steps from member accessors and whole-model delegates.
/// </summary>
public static class Sanitizers {
    /// <summary>
    /// Creates a member sanitization step that reads the value, applies the transforms in order,
    /// and writes the result back only when it changed.
    /// </summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <typeparam name="TValue">The member type.</typeparam>
    /// <param name="getter">Reads the member value.</param>
    /// <param name="memberName">The member name used in failure messages.</param>
    /// <param name="setter">Writes the sanitized value. Sanitization is in-place by design; a
    /// member without a setter cannot be sanitized.</param>
    /// <param name="transforms">The transforms applied in order.</param>
    /// <returns>The pipeline step.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">No transform is supplied.</exception>
    public static ISanitizationStep<T> Member<T, TValue>(
        Func<T, TValue?> getter,
        string memberName,
        Action<T, TValue?> setter,
        params Func<TValue?, TValue?>[] transforms) {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentException.ThrowIfNullOrEmpty(memberName);
        ArgumentNullException.ThrowIfNull(setter);
        ArgumentNullException.ThrowIfNull(transforms);

        if(transforms.Length is 0) {
            throw new ArgumentException("At least one sanitizer transform is required.", nameof(transforms));
        }

        return new MemberSanitizer<T, TValue>(getter, memberName, setter, transforms);
    }

    /// <summary>
    /// Adapts a whole-model delegate to a pipeline step.
    /// </summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="sanitize">The sanitization delegate.</param>
    /// <returns>The pipeline step.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sanitize"/> is null.</exception>
    public static ISanitizationStep<T> ForModel<T>(Action<T> sanitize) {
        ArgumentNullException.ThrowIfNull(sanitize);

        return new DelegateSanitizer<T>(sanitize);
    }

    /// <summary>
    /// Adapts a whole-model sanitizer to a pipeline step.
    /// </summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="sanitizer">The sanitizer.</param>
    /// <returns>The pipeline step.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sanitizer"/> is null.</exception>
    public static ISanitizationStep<T> ForModel<T>(ISanitizer<T> sanitizer) {
        ArgumentNullException.ThrowIfNull(sanitizer);

        return new DelegateSanitizer<T>(sanitizer.Sanitize);
    }

    private sealed class DelegateSanitizer<T>(Action<T> sanitize) : ISanitizationStep<T> {
        public void Sanitize(T instance) => sanitize(instance);
    }

    private sealed class MemberSanitizer<T, TValue>(
        Func<T, TValue?> getter,
        string memberName,
        Action<T, TValue?> setter,
        Func<TValue?, TValue?>[] transforms) : ISanitizationStep<T> {
        public void Sanitize(T instance) {
            try {
                var value = getter(instance);
                var sanitized = value;

                foreach(var transform in transforms) {
                    sanitized = transform(sanitized);
                }

                if(!EqualityComparer<TValue?>.Default.Equals(sanitized, value)) {
                    setter(instance, sanitized);
                }
            }
            catch(Exception exception) when(exception is not OperationCanceledException) {
                throw new ValidationException(
                    $"The sanitizer for member '{memberName}' threw {exception.GetType().Name}.",
                    exception);
            }
        }
    }
}
