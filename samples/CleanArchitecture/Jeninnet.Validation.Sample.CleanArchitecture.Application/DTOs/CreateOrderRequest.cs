//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.CleanArchitecture.Application.DTOs;

public sealed record CreateOrderRequest(
    string CustomerName,
    string Email,
    List<OrderLineDto> Lines);

public sealed record OrderLineDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice);
