//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.GeneratorModel;

/// <summary>
/// Represents a typed parameter for a validation rule.
/// </summary>
/// <param name="Value">The literal value of the parameter.</param>
/// <param name="TypeName">The fully-qualified name of the parameter type.</param>
internal sealed record RuleParameter(string? Value, string? TypeName);
