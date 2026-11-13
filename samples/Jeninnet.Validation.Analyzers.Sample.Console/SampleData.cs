//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Sample.Console;

public static class SampleData {
    public static Order CreateInvalidOrder() =>
        new() {
            Id = Guid.NewGuid(),
            OrderNumber = "", // invalid
            OrderDate = DateTime.UtcNow.AddDays(2), // future date
            DeliveryDate = DateTime.UtcNow.AddDays(-5), // before order date
            Customer = new Customer {
                Id = Guid.NewGuid(),
                FirstName = "",
                LastName = "",
                Email = "invalid-email",
                PhoneNumber = "12",
                Age = 10,
                BillingAddress = new Address {
                    Street = "",
                    City = "",
                    PostalCode = "",
                    Country = ""
                },
                ShippingAddress = null
            },
            Lines =
            [
                new() {
                    Id = Guid.NewGuid(),
                    Product = new() {
                        Id = Guid.NewGuid(),
                        Name = null!,
                        Code = "AB",
                        Supplier = new Supplier {
                            Id = Guid.NewGuid(),
                            Name = "",
                            CompanyName = "",
                            ContactPerson = "A",
                            Email = "bad-email",
                            PhoneNumber = "055066",
                            VatNumber = "12",
                            Rating = 10,
                            Address = null,

                            Website = "not-a-url",
                            LastLoginIP = "999.999.999.999",

                            Age = 10,
                            AgeOptional = 0,
                            AgeValue_RS = 11,
                            AgeValue_RS_Optional = null,
                            AgeValue_RC = 12,
                            AgeValue_RC_Optional = null
                        }
                        },
                        Quantity = 0,
                        UnitPrice = 123.456m, // precision 6, scale 3 (violates 18, 2)
                        DiscountPercentage = 150
                        }
                        ],
            TotalAmount = -10m,
        };

#pragma warning disable S2068
    public static User CreateInvalidUser() =>
    new() {
        Id = Guid.Empty,
        Username = "ab",
        Password = "short",
        Email = "invalid",
        RoleType = (UserRole)99
    };
#pragma warning restore S2068
}
