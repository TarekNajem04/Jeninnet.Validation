//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.AotSmoke;

/// <summary>
/// Attribute-driven model exercised by the NativeAOT smoke harness.
/// </summary>
[Validate]
public sealed class Order {
    /// <summary>Gets or sets the order reference (rule-set scoped).</summary>
    [NotNull]
    [NotEmpty]
    [RuleSet("strict")]
    public string? Reference { get; set; }

    /// <summary>Gets or sets the contact email.</summary>
    [NotEmpty]
    [NotNull]
    [Email]
    public string? Email { get; set; }

    /// <summary>Gets or sets the quantity.</summary>
    [InclusiveBetween(1, 100)]
    public int Quantity { get; set; }
}

/// <summary>
/// Flags used by the generated enum validation check.
/// </summary>
[Flags]
public enum OrderAccess {
    /// <summary>No access.</summary>
    None = 0,

    /// <summary>Read access.</summary>
    Read = 1,

    /// <summary>Write access.</summary>
    Write = 1 << 1,

    /// <summary>Delete access.</summary>
    Delete = 1 << 2,

    /// <summary>All access flags.</summary>
    All = Read | Write | Delete
}

/// <summary>
/// Model with a <c>[Flags]</c> enum validated by the generated validator.
/// </summary>
[Validate]
public sealed class FlagsOrder {
    /// <summary>Gets or sets the access flags.</summary>
    [Enum]
    public OrderAccess Access { get; set; }
}

/// <summary>
/// Model validated by a fluent validator (sync + async rules).
/// </summary>
public sealed class Shipment {
    /// <summary>Gets or sets the shipment code.</summary>
    public string? Code { get; set; }
}

/// <summary>
/// Model validated by an async-only fluent validator (strict-async guard).
/// </summary>
public sealed class AsyncOnlyRefund {
    /// <summary>Gets or sets the refund reason.</summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Serialization projection used to prove a source-generated JSON round-trip.
/// </summary>
/// <param name="Member">The member name.</param>
/// <param name="Message">The error message.</param>
internal sealed record ErrorProjection(string Member, string? Message);

/// <summary>
/// Source-generated JSON context (no runtime reflection).
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(ErrorProjection[]))]
internal sealed partial class SmokeJsonContext : JsonSerializerContext;
