//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Grpc.Tests;

[TestClass]
[DoNotParallelize]
public sealed class HostConformanceTests {
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_Grpc_InvalidRequest_DoesNotCallServiceAsync() {
        await using var app = await StartAppAsync(services =>
            services.AddSingleton<IValidator<CreateRequestMessage>, CreateRequestMessageValidator>());
        using var channel = CreateChannel(app);
        var client = new ValidationTest.ValidationTestClient(channel);
        ValidationTestService.Calls = 0;
        RpcException? caught = null;

        try {
            await client.CreateAsync(
                new CreateRequestMessage { Name = string.Empty, Email = "bad" },
                cancellationToken: TestContext.CancellationToken);
        }
        catch(RpcException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.StatusCode.Should().Be(StatusCode.InvalidArgument));
        MSTestAssertions.That(() => ValidationTestService.Calls.Should().Be(0));

        var detailsEntry = caught!.Trailers.Single(entry => entry.Key == "grpc-status-details-bin");
        var status = GoogleRpcStatus.Parser.ParseFrom(detailsEntry.ValueBytes);
        var badRequest = status.Details.Select(static detail => detail.Unpack<GoogleBadRequest>()).Single();

        MSTestAssertions.That(() => badRequest.FieldViolations.Should().HaveCount(2));
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_Grpc_MissingValidator_PassesThroughAsync() {
        await using var app = await StartAppAsync();
        using var channel = CreateChannel(app);
        var client = new ValidationTest.ValidationTestClient(channel);
        ValidationTestService.Calls = 0;

        var reply = await client.CreateAsync(
            new CreateRequestMessage { Name = string.Empty, Email = "bad" },
            cancellationToken: TestContext.CancellationToken);

        MSTestAssertions.That(() => reply.Name.Should().BeEmpty());
        MSTestAssertions.That(() => ValidationTestService.Calls.Should().Be(1));
    }

    [TestMethod]
    [TestCategory("HostConformance")]
    public async Task HostConformance_Grpc_AsyncOnlyInvalidRequest_DoesNotCallServiceAsync() {
        await using var app = await StartAppAsync(services =>
            services.AddSingleton<IValidator<AsyncRequestMessage>, AsyncRequestMessageValidator>());
        using var channel = CreateChannel(app);
        var client = new ValidationTest.ValidationTestClient(channel);
        ValidationTestService.Calls = 0;
        RpcException? caught = null;

        try {
            await client.AsyncCheckAsync(
                new AsyncRequestMessage { Code = string.Empty },
                cancellationToken: TestContext.CancellationToken);
        }
        catch(RpcException exception) {
            caught = exception;
        }

        MSTestAssertions.That(() => caught.Should().NotBeNull());
        MSTestAssertions.That(() => caught!.StatusCode.Should().Be(StatusCode.InvalidArgument));
        MSTestAssertions.That(() => ValidationTestService.Calls.Should().Be(0));
        MSTestAssertions.That(() => AsyncRequestMessageValidator.Captured.CanBeCanceled.Should().BeTrue());
    }

    private async Task<WebApplication> StartAppAsync(Action<IServiceCollection>? configure = null) {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc(options => options.Interceptors.Add<ValidationInterceptor>());
        builder.Services.AddJeninnetGrpcValidation();
        builder.Services.AddSingleton<ValidationTestService>();
        configure?.Invoke(builder.Services);

        var app = builder.Build();
        app.MapGrpcService<ValidationTestService>();
        await app.StartAsync(TestContext.CancellationToken);

        return app;
    }

    private static GrpcChannel CreateChannel(WebApplication app) {
        var httpClient = app.GetTestClient();

        return GrpcChannel.ForAddress(
            httpClient.BaseAddress!,
            new GrpcChannelOptions {
                HttpClient = httpClient,
                HttpVersion = new Version(2, 0)
            });
    }
}
