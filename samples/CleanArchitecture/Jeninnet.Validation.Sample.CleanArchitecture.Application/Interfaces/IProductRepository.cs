//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.CleanArchitecture.Application.Interfaces;

public interface IProductRepository {
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
}
