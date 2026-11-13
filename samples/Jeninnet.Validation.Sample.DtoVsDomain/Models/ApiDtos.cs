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
// API Layer — Data Transfer Objects
// DTOs carry input data across the boundary and should be
// validated as early as possible (at the API layer).
// ============================================================

public sealed record CreateUserDto(
    string Username,
    string Email,
    string Password,
    string ConfirmPassword,
    int Age);

public sealed record UpdateUserDto(
    string? Email,
    string? DisplayName);
