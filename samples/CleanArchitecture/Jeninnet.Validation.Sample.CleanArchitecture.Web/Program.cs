//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

var orderService = new OrderService(new CreateOrderValidator());

Console.WriteLine("=== CLEAN ARCHITECTURE SAMPLE ===");
Console.WriteLine();

// Valid order
var validOrder = new CreateOrderRequest("John Doe", "john@example.com",
    [new OrderLineDto("Widget", 2, 9.99m), new OrderLineDto("Gadget", 1, 29.99m)]);

var validResult = orderService.ValidateOrder(validOrder);
Console.WriteLine($"Valid order: {validResult.IsValid} (expected: True)");
Console.WriteLine();

// Invalid order
var invalidOrder = new CreateOrderRequest("", "bad-email",
    [new OrderLineDto("", 0, -1m)]);

try {
    orderService.CreateOrder(invalidOrder);
}
catch(Jeninnet.Validation.ValidationException ex) {
    Console.WriteLine($"Invalid order errors: {ex.Errors.Count}");
    foreach(var error in ex.Errors) {
        Console.WriteLine($"  [{error.MemberName}] {error.Message} (code: {error.ErrorCode})");
    }
}

// Product validation
var productValidator = new CreateProductValidator();
var validProduct = new CreateProductRequest("Laptop", "ELE-0010", 999.99m, 10);
var invalidProduct = new CreateProductRequest("", "bad", -5m, -1);

Console.WriteLine();
#pragma warning disable S6966
Console.WriteLine($"Valid product: {productValidator.Validate(validProduct).IsValid} (expected: True)");
Console.WriteLine($"Invalid product errors: {productValidator.Validate(invalidProduct).Errors.Count} (expected: 4+)");
