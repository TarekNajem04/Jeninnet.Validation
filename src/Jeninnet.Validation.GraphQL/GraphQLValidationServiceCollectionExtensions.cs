//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.GraphQL;

/// <summary>
/// Registration helpers for HotChocolate GraphQL input validation.
/// </summary>
public static class GraphQLValidationServiceCollectionExtensions {
    /// <summary>
    /// Adds the validation field middleware to the GraphQL request executor. Validators must be
    /// registered separately as <see cref="IValidator{T}"/>.
    /// </summary>
    /// <param name="builder">The GraphQL request executor builder.</param>
    /// <param name="configure">Optional options configuration.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRequestExecutorBuilder AddJeninnetGraphQLValidation(
        this IRequestExecutorBuilder builder,
        Action<GraphQLValidationOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new GraphQLValidationOptions();
        configure?.Invoke(options);

        builder.Services.AddSingleton(options);

        return builder.UseField<GraphQLValidationMiddleware>(
            static (_, next) => new GraphQLValidationMiddleware(next));
    }
}
