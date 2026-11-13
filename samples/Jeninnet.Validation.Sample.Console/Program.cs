//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
var services = new ServiceCollection();

// Register validators
services.AddSingleton<AddressValidator>();
services.AddSingleton<CustomerValidator>();
services.AddSingleton<UserValidator>();
services.AddSingleton<SupplierValidator>();
services.AddSingleton<OrderLineValidator>();
services.AddSingleton<OrderValidator>();

var provider = services.BuildServiceProvider();

OrderValidatorTask.Run(provider);

AdvancedFeaturesTask.Run();

Console.WriteLine();
Console.WriteLine("Done.");
