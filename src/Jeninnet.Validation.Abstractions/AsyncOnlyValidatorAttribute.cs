//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Marks a validator whose rules are all asynchronous.
/// </summary>
/// <remarks>
/// The source generator applies this marker to generated validators whose
/// synchronous rule set is empty while asynchronous rules exist. Calling the
/// synchronous <see cref="ISyncValidator{T}"/> API on a marked validator throws
/// <see cref="AsyncValidatorInvokedSynchronouslyException"/> at runtime, and the
/// analyzer diagnostic <c>VAL0016</c> reports such calls at compile time.
/// Hand-written validators may opt into the same detection by applying this
/// attribute explicitly.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AsyncOnlyValidatorAttribute : Attribute;
