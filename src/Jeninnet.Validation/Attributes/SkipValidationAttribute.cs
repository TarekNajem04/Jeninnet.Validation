//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Excludes a property, type, or parameter from generated validation. The source generator emits no
/// rules for a skipped property and no validator for a skipped type, including when the type is
/// reached through <c>[Nested]</c> or <c>[ForEach]</c>. Use this marker to combine Jeninnet
/// attributes with platform attributes or to turn off validation for a single member without
/// deleting its other annotations.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Parameter,
    Inherited = false,
    AllowMultiple = false)]
public sealed class SkipValidationAttribute : Attribute;
