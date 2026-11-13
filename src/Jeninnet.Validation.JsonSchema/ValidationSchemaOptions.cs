//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.JsonSchema;

/// <summary>
/// Options for JSON Schema generation.
/// </summary>
public sealed class ValidationSchemaOptions {
    /// <summary>
    /// Gets or sets the schema dialect URI.
    /// </summary>
    public string SchemaDialect { get; set; } = "https://json-schema.org/draft/2020-12/schema";

    /// <summary>
    /// Gets or sets a value indicating whether advisory rules are emitted as
    /// <c>x-validation-rules</c>. Defaults to <see langword="true"/>.
    /// </summary>
    public bool IncludeAdvisoryRules { get; set; } = true;
}
