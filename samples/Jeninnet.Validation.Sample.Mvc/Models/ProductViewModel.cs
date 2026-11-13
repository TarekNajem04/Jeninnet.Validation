//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Mvc.Models;

#pragma warning disable S6964

public sealed record ProductViewModel(
    string? Name,
    string? Sku,
    decimal Price,
    int Stock,
    string? Category,
    List<string>? Tags
);
