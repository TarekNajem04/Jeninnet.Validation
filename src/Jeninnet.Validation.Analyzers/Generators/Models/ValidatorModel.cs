//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Models;

/// <summary>
/// Represents all validation metadata for a single target type
/// while inside the generator.
/// </summary>
/// <param name="TargetTypeFullName">The full name of the target type.</param>
/// <param name="Rules">The list of rules for the validator.</param>
internal sealed record ValidatorModel(string TargetTypeFullName, IReadOnlyList<RuleModel> Rules);
