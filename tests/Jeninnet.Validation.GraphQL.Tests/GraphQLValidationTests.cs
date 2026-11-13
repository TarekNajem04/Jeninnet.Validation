//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.GraphQL.Tests;

// ---------------------------------------------------------------------------
// Inputs, resolvers, and validators
// ---------------------------------------------------------------------------

public sealed class CreateOrderInput {
    public string? Name { get; set; }

    public string? Email { get; set; }
}

public sealed class CreateOrderInputValidator : Validator<CreateOrderInput> {
    protected override void Configure(ValidatorRuleBuilder<CreateOrderInput> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(CreateOrderInput.Name))
               .NotEmpty()
               .WithMessage("Name is required.")
               .WithCode("NAME_REQUIRED");

        builder.RuleFor(static x => x.Email, nameof(CreateOrderInput.Email))
               .EmailAddress()
               .WithMessage("Email is invalid.")
               .WithCode("EMAIL_INVALID");
    }
}

[GraphQLValidationRuleSets("strict")]
public sealed class StrictOrderInput {
    public string? Name { get; set; }
}

public sealed class StrictOrderInputValidator : Validator<StrictOrderInput> {
    protected override void Configure(ValidatorRuleBuilder<StrictOrderInput> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(StrictOrderInput.Name))
               .NotEmpty();

        builder.RuleSet("strict", strict =>
            strict.RuleFor(static x => x.Name, nameof(StrictOrderInput.Name))
                  .MaximumLength(3));
    }
}

public sealed class AsyncOrderInput {
    public string? Code { get; set; }
}

public sealed class AsyncOrderInputValidator : Validator<AsyncOrderInput> {
    protected override void Configure(ValidatorRuleBuilder<AsyncOrderInput> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(AsyncOrderInput.Code))
               .Must(
                   static (value, _) => ValueTask.FromResult(!string.IsNullOrWhiteSpace(value)),
                   "Code is required.");
    }
}

public sealed class Query {
    private readonly string _pong = "pong";

    public string Ping() => _pong;
}

public sealed class Mutation(MutationCounter counter) {
    public string CreateOrder(CreateOrderInput input) {
        ArgumentNullException.ThrowIfNull(input);
        counter.CreateOrder++;
        return input.Name ?? string.Empty;
    }

    public string StrictOrder(StrictOrderInput input) {
        ArgumentNullException.ThrowIfNull(input);
        counter.StrictOrder++;
        return input.Name ?? string.Empty;
    }

    public string AsyncOrder(AsyncOrderInput input) {
        ArgumentNullException.ThrowIfNull(input);
        counter.AsyncOrder++;
        return input.Code ?? string.Empty;
    }
}

public sealed class MutationCounter {
    public int CreateOrder { get; set; }

    public int StrictOrder { get; set; }

    public int AsyncOrder { get; set; }

    public void Reset() {
        CreateOrder = 0;
        StrictOrder = 0;
        AsyncOrder = 0;
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class GraphQLValidationTests {
    public TestContext TestContext { get; set; } = null!;
    [TestMethod]
    public async Task GraphQL_InvalidInput_ReturnsErrorsWithMemberAndCodeAsync() {

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

        var nameError = errors.Single(error =>
            error!["extensions"]!["code"]!.GetValue<string>() == "NAME_REQUIRED");
        var emailError = errors.Single(error =>
            error!["extensions"]!["code"]!.GetValue<string>() == "EMAIL_INVALID");

        MSTestAssertions.That(() => nameError!["extensions"]!["member"]!.GetValue<string>().Should().Be("Name"));
        MSTestAssertions.That(() => emailError!["extensions"]!["member"]!.GetValue<string>().Should().Be("Email"));
    }

    [TestMethod]
    public async Task GraphQL_ValidInput_ReachesResolverAsync() {

        await using var provider = CreateProvider();
        var counter = provider.GetRequiredService<MutationCounter>();
        counter.Reset();
        var executor = await provider.GetRequestExecutorAsync(cancellationToken: TestContext.CancellationToken);

        var result = await executor.ExecuteAsync(
            "mutation { createOrder(input: { name: \"order-1\", email: \"buyer@example.com\" }) }",
            cancellationToken: TestContext.CancellationToken);
        var json = JsonNode.Parse(result.ToJson()) ?? throw new InvalidOperationException("Expected a JSON result.");

        MSTestAssertions.That(() => json["errors"].Should().BeNull());
        MSTestAssertions.That(() => json["data"]!["createOrder"]!.GetValue<string>().Should().Be("order-1"));
        MSTestAssertions.That(() => counter.CreateOrder.Should().Be(1));
    }

    [TestMethod]
    public async Task GraphQL_NoValidator_BypassesValidationAsync() {
        await using var provider = CreateProvider();
        var executor = await provider.GetRequestExecutorAsync(cancellationToken: TestContext.CancellationToken);

        var result = await executor.ExecuteAsync("{ ping }", cancellationToken: TestContext.CancellationToken);
        var json = JsonNode.Parse(result.ToJson()) ?? throw new InvalidOperationException("Expected a JSON result.");

        MSTestAssertions.That(() => json["errors"].Should().BeNull());
        MSTestAssertions.That(() => json["data"]!["ping"]!.GetValue<string>().Should().Be("pong"));
    }

    [TestMethod]
    public async Task GraphQL_AsyncValidator_RunsOnAsyncPathAsync() {

        await using var provider = CreateProvider();
        var counter = provider.GetRequiredService<MutationCounter>();
        counter.Reset();
        var executor = await provider.GetRequestExecutorAsync(cancellationToken: TestContext.CancellationToken);

        var result = await executor.ExecuteAsync("mutation { asyncOrder(input: { code: \"\" }) }", cancellationToken: TestContext.CancellationToken);
        var json = JsonNode.Parse(result.ToJson()) ?? throw new InvalidOperationException("Expected a JSON result.");

        MSTestAssertions.That(() => json["errors"]!.AsArray().Should().HaveCount(1));
        MSTestAssertions.That(() => counter.AsyncOrder.Should().Be(0));
    }

    [TestMethod]
    public async Task GraphQL_RuleSetsAttribute_SelectsStrictRulesAsync() {

        await using var provider = CreateProvider();
        var counter = provider.GetRequiredService<MutationCounter>();
        counter.Reset();
        var executor = await provider.GetRequestExecutorAsync(cancellationToken: TestContext.CancellationToken);

        var result = await executor.ExecuteAsync("mutation { strictOrder(input: { name: \"too-long\" }) }", cancellationToken: TestContext.CancellationToken);
        var json = JsonNode.Parse(result.ToJson()) ?? throw new InvalidOperationException("Expected a JSON result.");

        MSTestAssertions.That(() => json["errors"]!.AsArray().Should().HaveCount(1));
        MSTestAssertions.That(() => counter.StrictOrder.Should().Be(0));
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
        services.AddScoped<IValidator<StrictOrderInput>, StrictOrderInputValidator>();
        services.AddScoped<IValidator<AsyncOrderInput>, AsyncOrderInputValidator>();

        return services.BuildServiceProvider();
    }
}
