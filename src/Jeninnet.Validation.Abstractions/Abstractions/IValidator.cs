//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Abstractions;

/// <summary>
/// Represents a synchronous/asynchronous validator for a specific type.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public interface IValidator<in T> : ISyncValidator<T>, IAsyncValidator<T>;
