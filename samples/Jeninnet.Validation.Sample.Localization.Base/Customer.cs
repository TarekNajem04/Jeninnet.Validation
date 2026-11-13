//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Base;

/// <summary>
/// Represents a customer with basic information.
/// </summary>
/// <param name="Name">The name of the customer.</param>
/// <param name="Email">The email address of the customer.</param>
/// <param name="Age">The age of the customer.s</param>
public sealed record Customer(string Name, string Email, int Age) {
    public static Customer Invalid { get; } = new(Name: "", Email: "bad-email", Age: 10);
}
