//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Composite;

public sealed class CultureFilter(IInnerMessageProvider inner, string[] allowedCultures) : IInnerMessageProvider {
    private readonly HashSet<string> _cultures = [.. allowedCultures];

    public async ValueTask<string?> GetMessageTemplateAsync(string key, CultureInfo culture, CancellationToken cancellationToken = default) {
        var current = culture;
        while(current is not null) {
            if(_cultures.Contains(current.Name)) {
                return await inner.GetMessageTemplateAsync(key, culture, cancellationToken);
            }

            if(current == CultureInfo.InvariantCulture) {
                break;
            }

            current = current.Parent;
        }

        return null;
    }
}
