//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
// Build DI container

var services = new ServiceCollection();

// Register validators
services.AddGeneratedValidators(typeof(Program).Assembly);

var provider = services.BuildServiceProvider();

await OrderValidatorTask.RunAsync(provider);
await UserValidatorTask.RunAsync(provider);

Console.WriteLine();
Console.WriteLine("Done.");
