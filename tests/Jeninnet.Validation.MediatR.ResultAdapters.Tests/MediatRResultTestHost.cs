//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR.ResultAdapters.Tests;

internal static class MediatRResultTestHost {
    internal static ServiceProvider Create(
        Action<IServiceCollection>? configure = null,
        Action<MediatRValidationOptions>? configureOptions = null) {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining(typeof(MediatRResultTestHost)));
        services.AddValidationPipeline(configureOptions);
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
