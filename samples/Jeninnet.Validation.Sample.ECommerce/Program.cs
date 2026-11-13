//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Platform/
// Licensed under the MIT License.
//

Console.WriteLine("=== E-COMMERCE SAMPLE ===");
Console.WriteLine("Demonstrates SetValidator, RuleForEach, ChildRules, Include,");
Console.WriteLine("and nested validation with full property paths.");
Console.WriteLine();

// =============================================================
// Part 1: Nested property paths in validation results
// =============================================================
Console.WriteLine("--- Part 1: Nested Validation with Property Paths ---");
Console.WriteLine("Expected paths: Order.Customer.Address.City, Order.Lines[2].ProductName");
Console.WriteLine();

var orderValidator = new OrderValidator();

var invalidOrder = new Order(
    OrderNumber: "BAD",
    Customer: new Customer(
        Name: "",
        Email: "not-email",
        Address: new Address(
            Street: "",
            City: "",
            ZipCode: "abc",
            Country: "")),
    Lines:
    [
        new OrderLine(ProductName: "", Quantity: 0, UnitPrice: -1m),
        new OrderLine(ProductName: "Valid Product", Quantity: 5, UnitPrice: 10m),
        new OrderLine(ProductName: "", Quantity: -3, UnitPrice: 0m),
    ],
    Payment: new Payment(Method: "Unknown", TransactionId: "", Amount: -50m));

var result = await orderValidator.ValidateAsync(invalidOrder);

Console.WriteLine("Validation errors (showing full property paths):");
foreach (var err in result.Errors)
    Console.WriteLine($"  FAIL  {err.MemberName}: {err.Message}");

Console.WriteLine();

// =============================================================
// Part 2: Demonstrating each feature
// =============================================================
Console.WriteLine("--- Part 2: Feature Breakdown ---");

// SetValidator
Console.WriteLine("SetValidator — AddressValidator reused inside CustomerValidator:");
var addrCheck = await new AddressValidator().ValidateAsync(new Address("", "", "bad", ""));
foreach (var err in addrCheck.Errors)
    Console.WriteLine($"  [{err.MemberName}] {err.Message}");
Console.WriteLine();

// RuleForEach
Console.WriteLine("RuleForEach — validates each OrderLine:");
var lineCheck = await new OrderLineValidator().ValidateAsync(new OrderLine("", 0, -5m));
foreach (var err in lineCheck.Errors)
    Console.WriteLine($"  [{err.MemberName}] {err.Message}");
Console.WriteLine();

// ChildRules
Console.WriteLine("ChildRules — inline nested validation for Payment:");
var paymentCheck = await new OrderValidator()
    .ValidateAsync(new Order("ORD-000001", new Customer("A", "a@b.com",
        new Address("S", "C", "1234", "US")), [], new Payment("", "", 0m)));
foreach (var err in paymentCheck.Errors)
    Console.WriteLine($"  [{err.MemberName}] {err.Message}");
Console.WriteLine();

// Include
Console.WriteLine("Include — adds extra OrderNumber rules inline.");
Console.WriteLine();

// =============================================================
// Part 3: Selective validation
// =============================================================
Console.WriteLine("--- Part 3: Valid Order (all rules pass) ---");

var validOrder = new Order(
    OrderNumber: "ORD-123456",
    Customer: new Customer(
        Name: "Jane Doe",
        Email: "jane@example.com",
        Address: new Address(
            Street: "123 Main St",
            City: "New York",
            ZipCode: "10001",
            Country: "USA")),
    Lines:
    [
        new OrderLine(ProductName: "Widget", Quantity: 2, UnitPrice: 19.99m),
        new OrderLine(ProductName: "Gadget", Quantity: 1, UnitPrice: 49.99m),
    ],
    Payment: new Payment(Method: "CreditCard", TransactionId: "txn_001", Amount: 89.97m));

var validResult = await orderValidator.ValidateAsync(validOrder);
Console.WriteLine($"  Valid: {validResult.IsValid} ({(validResult.IsValid ? "PASS" : "FAIL")})");
Console.WriteLine($"  Errors: {validResult.Errors.Count}");

Console.WriteLine();
Console.WriteLine("Done.");
