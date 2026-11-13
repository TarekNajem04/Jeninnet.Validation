//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.CleanArchitecture.Infrastructure.Repositories;

public sealed class InMemoryProductRepository : IProductRepository {
    private readonly List<Product> _products = [];

    public Task<Product?> GetByIdAsync(Guid id)
        => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task AddAsync(Product product) {
        _products.Add(product);
        return Task.CompletedTask;
    }
}
