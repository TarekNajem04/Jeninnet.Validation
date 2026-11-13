//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Sqlite;

public sealed class EmployeeValidator : Validator<Employee> {
    protected override void Configure(ValidatorRuleBuilder<Employee> builder) {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static x => x.Name, "Name").NotEmpty();
        builder.RuleFor(static x => x.Email, "Email").NotEmpty().EmailAddress();
        builder.RuleFor(static x => x.Department, "Department").NotEmpty();
    }
}
