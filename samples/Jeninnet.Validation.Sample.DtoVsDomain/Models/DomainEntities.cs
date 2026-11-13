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
namespace Jeninnet.Validation.Sample.DtoVsDomain.Models;

// ============================================================
// Application / Domain Layer — Domain Entities
// Domain entities should NOT contain validation logic.
// They represent pure data and business identity.
// ============================================================

public sealed record User(
    string Id,
    string Username,
    string Email,
    string PasswordHash,
    int Age,
    DateTime CreatedAt);

public sealed record Address(
    string Street,
    string City,
    string Country);
