//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Sample.MediatR.Handlers;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderResponse> {
    public Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        var order = request.Request;
        var total = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
        return Task.FromResult(new OrderResponse(Guid.NewGuid(), order.CustomerName, total));
    }
}
