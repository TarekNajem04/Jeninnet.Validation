//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Nested;

// ---------------------------------------------------------------------------
// ForEachRule — indexed path propagation
// ---------------------------------------------------------------------------

internal sealed class Order {
    public List<string>? Tags { get; init; }
    public List<Address>? Lines { get; init; }
}
