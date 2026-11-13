//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Platform/
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.ECommerce;

public sealed record Address(
    string Street,
    string City,
    string ZipCode,
    string Country);

public sealed record Customer(
    string Name,
    string Email,
    Address Address);

public sealed record OrderLine(
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record Payment(
    string Method,
    string TransactionId,
    decimal Amount);

public sealed record Order(
    string OrderNumber,
    Customer Customer,
    List<OrderLine> Lines,
    Payment Payment);
