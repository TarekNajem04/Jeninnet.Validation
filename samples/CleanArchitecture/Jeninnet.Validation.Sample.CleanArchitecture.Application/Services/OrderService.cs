//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Sample.CleanArchitecture.Application.Services;

public sealed class OrderService(CreateOrderValidator validator) {
    public ValidationResult ValidateOrder(CreateOrderRequest request)
        => validator.Validate(request);

    public Order CreateOrder(CreateOrderRequest request) {
        ArgumentNullException.ThrowIfNull(request);
        validator.ValidateAndThrow(request);
        return new Order {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            Email = request.Email,
            OrderDate = DateTime.UtcNow,
            Lines = [
                .. request.Lines.Select(
                    l => new OrderLine {
                        ProductName = l.ProductName,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice
                    }
                )
            ]
        };
    }
}
