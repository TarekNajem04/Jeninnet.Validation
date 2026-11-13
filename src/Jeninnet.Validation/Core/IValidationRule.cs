//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Defines a combined synchronous and asynchronous validation rule.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public interface IValidationRule<T> : ISyncValidationRule<T>, IAsyncValidationRule<T>;
