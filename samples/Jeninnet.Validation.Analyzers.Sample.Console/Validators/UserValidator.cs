//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Sample.Domain.DataTransfareObjects.Validators;

public sealed partial class UserValidator {
    partial void AddCustomValidators() {
        _syncRules.Add(
            new MustRule<User, string?>(
                ValidatorFactory.Accessor<User, string?>(static x => x.Password, nameof(User.Password)),
                static p => p?.Any(char.IsDigit) is true,
                "Password must contain at least one digit."
            )
        );

        _syncRules.Add(
            new MustRule<User, string?>(
                ValidatorFactory.Accessor<User, string?>(static x => x.Password, nameof(User.Password)),
                static p => p?.Any(char.IsUpper) is true,
                "Password must contain at least one uppercase letter."
            )
        );
    }
}
