//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Grpc;

/// <summary>
/// Configures the gRPC validation interceptor.
/// </summary>
public sealed class GrpcValidationOptions {
    /// <summary>
    /// Gets the default rule sets applied when a request type has no
    /// <see cref="GrpcValidationRuleSetsAttribute"/>. Empty means all rules.
    /// </summary>
    public IList<string> RuleSets { get; } = [];
}
