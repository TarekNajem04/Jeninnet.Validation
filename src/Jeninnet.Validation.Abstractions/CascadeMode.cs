//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Specifies how validation should proceed when a rule fails.
/// </summary>
public enum CascadeMode {
    /// <summary>
    /// Validation continues even if a rule fails.
    /// </summary>
    Continue = 0,

    /// <summary>
    /// Validation stops for the current rule group when a rule fails.
    /// </summary>
    Stop = 1
}
