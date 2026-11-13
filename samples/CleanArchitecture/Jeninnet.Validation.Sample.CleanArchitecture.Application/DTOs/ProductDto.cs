//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.CleanArchitecture.Application.DTOs;

public sealed record CreateProductRequest(
    string Name,
    string Sku,
    decimal Price,
    int Stock);
