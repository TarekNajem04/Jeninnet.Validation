//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.CleanArchitecture.Domain.ValueObjects;

public readonly record struct Money(decimal Amount, string Currency) {
    public static Money Zero(string currency = "USD") => new(0, currency);

    public override string ToString() => $"{Currency} {Amount:F2}";
}
