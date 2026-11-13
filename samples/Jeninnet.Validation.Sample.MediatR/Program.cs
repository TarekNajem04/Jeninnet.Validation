//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
var services = new ServiceCollection();

services.AddLogging();
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateOrderHandler).Assembly));
services.AddValidationPipeline();
services.AddSingleton<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();

Console.WriteLine("=== CQRS + MEDIATR SAMPLE ===");
Console.WriteLine();

// Valid command
var valid = new CreateOrderCommand(new CreateOrderRequest(
    "John Doe", "john@example.com",
    [new OrderLineDto("Widget", 2, 9.99m)]));

var response = await mediator.Send(valid);
Console.WriteLine($"Valid order: {response.Total:C} (expected: {2 * 9.99m:C})");
Console.WriteLine();

// Invalid command — ValidationBehavior will throw
var invalid = new CreateOrderCommand(new CreateOrderRequest(
    "", "bad-email",
    [new OrderLineDto("", 0, -1m)]));

try {
    await mediator.Send(invalid);
}
catch(Jeninnet.Validation.ValidationException ex) {
    Console.WriteLine($"Invalid order errors: {ex.Errors.Count}");
    foreach(var error in ex.Errors) {
        Console.WriteLine($"  [{error.MemberName}] {error.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("Done.");
