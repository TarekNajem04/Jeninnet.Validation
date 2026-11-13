//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Named password policies available to the declarative attribute path.
/// </summary>
public enum PasswordStrengthPreset {
    /// <summary>Eight characters with letters and digits.</summary>
    Basic = 0,

    /// <summary>Twelve characters with upper-case, lower-case, digit, and symbol requirements.</summary>
    Strong = 1,

    /// <summary>Sixteen characters with upper-case, lower-case, digit, and symbol requirements.</summary>
    Strict = 2
}
