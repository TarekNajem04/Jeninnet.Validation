//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
using Microsoft.Extensions.Options;

namespace Jeninnet.Validation.AspNetCore.Tests;

/// <summary>Smoke coverage for the ASP.NET Core and OpenAPI registration helpers.</summary>
[TestClass]
public sealed class RegistrationExtensionsTests {
    [TestMethod]
    public void AddMvcValidation_RegistersOptionsFilterAndMvcFilter() {
        var services = new ServiceCollection();
        var result = services.AddMvcValidation(static options => options.IncludeErrorCodes = true);

        MSTestAssertions.That(() => ReferenceEquals(result, services).Should().BeTrue());

        using var provider = services.BuildServiceProvider();
        MSTestAssertions.That(() => provider.GetRequiredService<MvcValidationOptions>().IncludeErrorCodes.Should().BeTrue());
        MSTestAssertions.That(() => provider.GetRequiredService<JeninnetValidationFilter>().Should().NotBeNull());

        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        MSTestAssertions.That(() => mvcOptions.Filters.Should().NotBeEmpty());
    }

    [TestMethod]
    public void AddSchemaConstraintsFromValidators_RegistersRegistryAndTransformer() {
        var services = new ServiceCollection();
        var result = services.AddSchemaConstraintsFromValidators();

        MSTestAssertions.That(() => ReferenceEquals(result, services).Should().BeTrue());

        using var provider = services.BuildServiceProvider();
        MSTestAssertions.That(() => provider.GetRequiredService<ValidationSchemaRegistry>().Should().NotBeNull());
        MSTestAssertions.That(() => provider.GetRequiredService<ValidationSchemaTransformer>().Should().NotBeNull());
        MSTestAssertions.That(() => provider.GetRequiredService<IOptions<OpenApiOptions>>().Value.Should().NotBeNull());
    }

    [TestMethod]
    public void AddSchemaConstraintsFromValidators_OnOptions_ReturnsSameOptions() {
        var options = new OpenApiOptions();
        var result = options.AddSchemaConstraintsFromValidators();

        MSTestAssertions.That(() => ReferenceEquals(result, options).Should().BeTrue());
    }
}
