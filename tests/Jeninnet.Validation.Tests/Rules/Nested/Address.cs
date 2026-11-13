//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Nested;

// ---------------------------------------------------------------------------
// Nested validator rule — path propagation
// ---------------------------------------------------------------------------

internal sealed class Address {
    public string? Street { get; init; }
    public string? City { get; init; }
}
