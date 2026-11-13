//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A rule for ensuring that a string member has one of the configured file extensions. Matching is
/// ordinal/ordinal-ignore-case over spans and does not allocate.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="extensions">The allowed file extensions (with or without a leading dot).</param>
/// <param name="ignoreCase">Whether extension matching ignores case.</param>
/// <param name="allowNonStandard">Whether values without an extension are accepted.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class FileExtensionsRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    IEnumerable<string> extensions,
    bool ignoreCase = true,
    bool allowNonStandard = false,
    string? message = null,
    string? messageKey = ValidationMessageKeys.FILE_EXTENSIONS)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    private readonly string[] _extensions = Normalize(extensions);
    private readonly StringComparison _comparison = ignoreCase
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
    private readonly bool _allowNonStandard = allowNonStandard;

    /// <summary>
    /// Gets the normalized allowed extensions (each with a leading dot).
    /// </summary>
    public IReadOnlyList<string> Extensions => _extensions;

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not string text) {
            return;
        }

        var extension = Path.GetExtension(text.AsSpan());

        if(extension.IsEmpty) {
            if(!_allowNonStandard) {
                AddError(errors, context);
            }

            return;
        }

        for(var i = 0; i < _extensions.Length; i++) {
            if(extension.Equals(_extensions[i].AsSpan(), _comparison)) {
                return;
            }
        }

        AddError(errors, context);
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) {
        var joined = string.Join(", ", _extensions);

        return new Dictionary<string, object> {
            { "0", joined },
            { MessageParameterNames.Extensions, joined },
            { MessageParameterNames.PropertyValue, Accessor.GetValue(context.Instance)! }
        };
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> { { MessageParameterNames.Extensions, string.Join(", ", _extensions) } };

    private static string[] Normalize(IEnumerable<string> extensions) {
        ArgumentNullException.ThrowIfNull(extensions);

        var normalized = new List<string>();

        foreach(var extension in extensions) {
            if(string.IsNullOrWhiteSpace(extension)) {
                continue;
            }

            var trimmed = extension.Trim();
            normalized.Add(trimmed.StartsWith('.') ? trimmed : "." + trimmed);
        }

        if(normalized.Count is 0) {
            throw new ArgumentException("At least one file extension must be provided.", nameof(extensions));
        }

        return [.. normalized];
    }
}
