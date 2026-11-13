//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.OpenApi;

/// <summary>
/// Applies validator descriptor constraints to OpenAPI schemas.
/// </summary>
/// <param name="registry">The descriptor registry.</param>
public sealed class ValidationSchemaTransformer(ValidationSchemaRegistry registry) : IOpenApiSchemaTransformer {
    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if(!registry.TryGet(context.JsonTypeInfo.Type, out var descriptor) || descriptor is null) {
            return Task.CompletedTask;
        }

        if(context.JsonPropertyInfo is null) {
            ApplyRequiredMembers(schema, descriptor, context);
            return Task.CompletedTask;
        }

        var memberName = context.JsonPropertyInfo.Name;
        var member = FindMember(descriptor, context.JsonTypeInfo, memberName);

        if(member is not null) {
            ValidationSchemaApplier.Apply(
                ValidationSchemaMapper.MapMember(member),
                schema,
                context.JsonTypeInfo.Options);
        }

        return Task.CompletedTask;
    }

    private static IValidationMemberDescriptor? FindMember(
        IValidationDescriptor descriptor,
        JsonTypeInfo typeInfo,
        string serializedName) {
        foreach(var member in descriptor.Members) {
            var resolvedName = JsonMemberNameMap.Resolve(typeInfo, member.Name);

            if(string.Equals(resolvedName, serializedName, StringComparison.Ordinal) ||
               string.Equals(member.Name, serializedName, StringComparison.Ordinal) ||
               string.Equals(resolvedName, serializedName, StringComparison.OrdinalIgnoreCase)) {
                return member;
            }
        }

        return null;
    }

    private static void ApplyRequiredMembers(
        OpenApiSchema schema,
        IValidationDescriptor descriptor,
        OpenApiSchemaTransformerContext context) {
        var required = descriptor.Members
            .Where(static member => ValidationSchemaMapper.MapMember(member).IsRequired)
            .Select(member => JsonMemberNameMap.Resolve(context.JsonTypeInfo, member.Name))
            .Where(static name => name is not null)
            .Select(static name => name!)
            .ToHashSet(StringComparer.Ordinal);

        if(required.Count is 0) {
            return;
        }

        schema.Required ??= new HashSet<string>(StringComparer.Ordinal);

        foreach(var name in required) {
            schema.Required.Add(name);
        }
    }
}
