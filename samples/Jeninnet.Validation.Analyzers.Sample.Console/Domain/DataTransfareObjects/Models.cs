//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#nullable enable
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Sample.Domain.DataTransfareObjects;

[Validate]
public sealed class Address {
    [NotEmpty, Length(2, 100, "Street must be between 2 and 100 characters.")]
    public string Street { get; init; } = string.Empty;

    [NotEmpty, Length(2, 50)]
    public string City { get; init; } = string.Empty;

    [NotEmpty, Length(3, 10)]
    public string PostalCode { get; init; } = string.Empty;

    [NotEmpty, Length(2, 50)]
    public string Country { get; init; } = string.Empty;
}

[Validate]
public sealed class Customer {
    [NotEmpty] public Guid Id { get; init; }
    [NotEmpty, Length(2, 50)] public string FirstName { get; init; } = string.Empty;
    [NotEmpty, Length(2, 50)] public string LastName { get; init; } = string.Empty;
    [NotNull, After("2025-12-28")]
    public DateTime? DateOfBirth { get; init; }
    [NotEmpty, Email] public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    [InclusiveBetween(18, 35)] public int Age { get; init; }
    [NotNull, Nested]
    public Address? BillingAddress { get; init; }
    [NotNull, Nested]
    public Address? ShippingAddress { get; init; }
}

public enum UserRole {
    Admin = 0,
    Moderator = 1,
    User = 2,
    Guest = 3
}

[Validate]
public sealed class User {
    [NotEmpty] public Guid Id { get; init; }
    [NotEmpty, Length(3, 50)]
    public string Username { get; init; } = string.Empty;
    [NotEmpty]
    [MinimumLength(8, "Password must be at least 8 characters.")]
    [MaximumLength(20, "Password must not exceed 20 characters.")]
    public string Password { get; init; } = string.Empty;
    [NotNull, NotEmpty, Email] public string? Email { get; init; }
    public bool IsActive { get; init; }
    [After("2025-12-28")]
    public DateTime CreatedAt { get; init; }
    [NotNull, NotEmpty]
    public string? Role { get; init; }

    [Enum]
    public UserRole RoleType { get; init; }
}

[Validate]
public sealed class Supplier {
    [NotEmpty] public Guid Id { get; init; }
    [NotEmpty, NotNull] public string Name { get; init; } = string.Empty;
    [NotEmpty, Length(2, 100), Equal("Microsoft")] public string CompanyName { get; init; } = string.Empty;
    [NotNull, NotEmpty, Length(2, 100)]
    public string? ContactPerson { get; init; }
    [NotNull, NotEmpty, Email] public string? Email { get; init; }

    [NotNull, NotEmpty, Url]
    public string? Website { get; init; }

    [NotNull, NotEmpty, IPAddress]
    public string? LastLoginIP { get; init; }

    [GreaterThan(18)] public int Age { get; init; }
    [GreaterThan(18)]
    public int AgeOptional { get; init; }
    [GreaterThan(18)] public AgeValueRecordStruct AgeValue_RS { get; init; }
    [NotNull, GreaterThan(18)] public AgeValueRecordStruct? AgeValue_RS_Optional { get; init; }
    [GreaterThan(18)] public AgeValueRecordClass AgeValue_RC { get; init; } = null!;
    [NotNull, GreaterThan(18)] public AgeValueRecordClass? AgeValue_RC_Optional { get; init; }

    [NotNull, NotEmpty, NotEqual(value: "068025")]
    public string? PhoneNumber { get; init; }

    public string? VatNumber { get; init; }

    [InclusiveBetween(0, 5)]
    public decimal Rating { get; init; }

    [NotNull, Nested]
    public Address? Address { get; init; }

    [GreaterThan(10)]
    // [LessThan(5)]
    public int Number { get; init; }
}

[Validate]
public sealed class Product {
    [NotEmpty]
    public Guid Id { get; init; }
    [NotNull]
    [NotEmpty]
    public string Name { get; init; } = string.Empty;
    [NotNull]
    [NotEmpty]
    [Length(3, 100)]
    public string? Code { get; init; }

    [NotNull, Nested]
    public Supplier? Supplier { get; init; }
}

[Validate]
public sealed class OrderLine {
    [NotEmpty] public Guid Id { get; init; }
    [NotNull]
    [Nested]
    public Product? Product { get; init; }
    [GreaterThan(0)] public int Quantity { get; init; }
    [GreaterThanOrEqual(0), ScalePrecision(18, 2)] public decimal UnitPrice { get; init; }
    [InclusiveBetween(0, 100)] public decimal DiscountPercentage { get; init; }
}

[Validate]
public sealed class Order {
    [NotEmpty] public Guid Id { get; init; }
    [NotEmpty] public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public DateTime? DeliveryDate { get; init; }

    [NotNull, Nested]
    public Customer? Customer { get; init; }

    [ForEach][NotEmpty] public List<OrderLine> Lines { get; init; } = [];

    /// <summary>Example: only validate TotalAmount in "Billing" rule set.</summary>
    [RuleSet("Billing")]
    [GreaterThanOrEqual(0)] public decimal TotalAmount { get; init; }
}
