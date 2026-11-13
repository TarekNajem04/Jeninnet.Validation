//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Common;

// ---------------------------------------------------------------------------
// Shared domain model used across common-rule tests.
// ---------------------------------------------------------------------------

internal sealed class Person {
    public string? Name { get; init; }
    public string? Email { get; init; }
    public Guid Id { get; init; }
    public int? Age { get; init; }
    public List<string>? Tags { get; init; }
}
