//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.GraphQL.Tests;

[TestClass]
[DoNotParallelize]
public sealed class HostConformanceTests {
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_GraphQL_InvalidInput_DoesNotCallResolverAsync() {
        await using var provider = CreateProvider();
        var counter = provider.GetRequiredService<MutationCounter>();
        counter.Reset();
        var executor = await provider.GetRequestExecutorAsync(cancellationToken: TestContext.CancellationToken);

        var result = await executor.ExecuteAsync(
            "mutation { createOrder(input: { name: \"\", email: \"bad\" }) }",
            cancellationToken: TestContext.CancellationToken);
        var json = JsonNode.Parse(result.ToJson()) ?? throw new InvalidOperationException("Expected a JSON result.");
        var errors = json["errors"]!.AsArray();

        MSTestAssertions.That(() => errors.Should().HaveCount(2));
        MSTestAssertions.That(() => counter.CreateOrder.Should().Be(0));
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_GraphQL_MissingValidator_PassesThroughAsync() {
        await using var provider = CreateProvider();
        var executor = await provider.GetRequestExecutorAsync(cancellationToken: TestContext.CancellationToken);

        var result = await executor.ExecuteAsync(
            "{ ping }",
            cancellationToken: TestContext.CancellationToken);
        var json = JsonNode.Parse(result.ToJson()) ?? throw new InvalidOperationException("Expected a JSON result.");

        MSTestAssertions.That(() => json["errors"].Should().BeNull());
        MSTestAssertions.That(() => json["data"]!["ping"]!.GetValue<string>().Should().Be("pong"));
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_GraphQL_AsyncOnlyInvalidInput_DoesNotCallResolverAsync() {
        await using var provider = CreateProvider();
        var counter = provider.GetRequiredService<MutationCounter>();
        counter.Reset();
        var executor = await provider.GetRequestExecutorAsync(cancellationToken: TestContext.CancellationToken);

        var result = await executor.ExecuteAsync(
            "mutation { asyncOrder(input: { code: \"\" }) }",
            cancellationToken: TestContext.CancellationToken);
        var json = JsonNode.Parse(result.ToJson()) ?? throw new InvalidOperationException("Expected a JSON result.");

        MSTestAssertions.That(() => json["errors"]!.AsArray().Should().HaveCount(1));
        MSTestAssertions.That(() => counter.AsyncOrder.Should().Be(0));
    }

    private static ServiceProvider CreateProvider() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGraphQLServer()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddJeninnetGraphQLValidation();
        services.AddSingleton<MutationCounter>();
        services.AddScoped<IValidator<CreateOrderInput>, CreateOrderInputValidator>();
        services.AddScoped<IValidator<AsyncOrderInput>, AsyncOrderInputValidator>();

        return services.BuildServiceProvider();
    }
}
