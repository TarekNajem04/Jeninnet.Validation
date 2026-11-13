//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Localization.Base;

/// <summary>
/// Represents a registration model with username, email, and password properties.
/// </summary>
/// <param name="Name">The name for the registration.</param>
/// <param name="Email">The email address for the registration.</param>
/// <param name="Password">The password for the registration.</param>
/// <param name="Age">The age of the customer.s</param>
public sealed record User(string Name, string Email, string Password, int Age) {
    public static User Invalid { get; } = new User(Name: "", Email: "bad-email", Password: "pw", Age: 13);
}
