//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Grpc;

/// <summary>
/// Validates unary and server-streaming gRPC request messages with the registered
/// <see cref="IValidator{T}"/> implementations and returns an <c>InvalidArgument</c> status
/// with <c>google.rpc.BadRequest</c> details when validation fails.
/// </summary>
public sealed class ValidationInterceptor : Interceptor {
    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation) {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        var result = await ValidateAsync(request, context).ConfigureAwait(false);

        if(result is { } failure) {
            throw CreateException(failure);
        }

        return await continuation(request, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation) {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        var result = await ValidateAsync(request, context).ConfigureAwait(false);

        if(result is { } failure) {
            throw CreateException(failure);
        }

        await continuation(request, responseStream, context).ConfigureAwait(false);
    }

    private static async ValueTask<ValidationResult?> ValidateAsync<TRequest>(
        TRequest request,
        ServerCallContext context) {
        var services = context.GetHttpContext().RequestServices;
        var validators = services.GetServices<IValidator<TRequest>>().ToList();

        if(validators.Count == 0) {
            return null;
        }

        var ruleSets = GrpcValidationRuleSetsCache.Get(typeof(TRequest));

        if(ruleSets.Count == 0) {
            var options = services.GetService<GrpcValidationOptions>();

            if(options is not null) {
                ruleSets = [.. options.RuleSets];
            }
        }

        var collector = new ValidationErrorCollector();
        var hasErrors = false;

        foreach(var validator in validators) {
            var result = ruleSets.Count > 0
                ? await validator.ValidateAsync(request, ruleSets, context.CancellationToken).ConfigureAwait(false)
                : await validator.ValidateAsync(request, context.CancellationToken).ConfigureAwait(false);

            if(!result.IsValid) {
                collector.AddRange(result.Errors);
                hasErrors = true;
            }
        }

        return hasErrors ? ValidationResult.FromErrors(collector) : null;
    }

    private static RpcException CreateException(ValidationResult result) {
        var badRequest = new GoogleBadRequest();

        foreach(var error in result.Errors) {
            badRequest.FieldViolations.Add(new GoogleBadRequest.Types.FieldViolation {
                Field = error.MemberName ?? string.Empty,
                Description = GetMessage(error)
            });
        }

        var message = string.Join("; ", result.Errors.Select(GetMessage));
        var status = new GoogleRpcStatus {
            Code = (int)StatusCode.InvalidArgument,
            Message = message
        };
        status.Details.Add(GoogleAny.Pack(badRequest));

        var trailers = new GrpcMetadata {
            { "grpc-status-details-bin", status.ToByteArray() }
        };

        return new RpcException(new Status(StatusCode.InvalidArgument, message), trailers);
    }

    private static string GetMessage(ValidationError error) {
        var message = error.Message;

        if(string.IsNullOrWhiteSpace(message)) {
            message = error.MessageKey;
        }

        if(string.IsNullOrWhiteSpace(message)) {
            message = error.MessageId.Id;
        }

        return string.IsNullOrWhiteSpace(message) ? "Invalid" : message;
    }
}

internal static class GrpcValidationRuleSetsCache {
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<string>> _cache = new();

    internal static IReadOnlyList<string> Get(Type requestType) =>
        _cache.GetOrAdd(
            requestType,
            static type => type.GetCustomAttribute<GrpcValidationRuleSetsAttribute>(inherit: true)?.RuleSets ?? []);
}
