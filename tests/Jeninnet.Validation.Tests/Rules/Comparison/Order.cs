//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.Comparison;

// ---------------------------------------------------------------------------
// Domain stub
// ---------------------------------------------------------------------------

internal sealed class Order {
    public int Quantity { get; init; }
    public int? OptionalQuantity { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public decimal TotalAmount { get; init; }
    public string? Status { get; init; }
    public string? Code { get; init; }
}
