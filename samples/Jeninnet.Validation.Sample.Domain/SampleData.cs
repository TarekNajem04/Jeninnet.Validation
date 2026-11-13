//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Domain;

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

                            Age = 10,
                            AgeOptional = null,
                            AgeValue_RS = 11,
                            AgeValue_RS_Optional = null,
                            AgeValue_RC = 12,
                            AgeValue_RC_Optional = null
                        }
                    },
                    Quantity = 0,
                    UnitPrice = -5,
                    DiscountPercentage = 150
                }
            ],
            TotalAmount = -10m,
        };
}
