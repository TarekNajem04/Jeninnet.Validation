//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.MinimalApi.Models;

public sealed record CreateUserRequest(
    string Username,
    string Email,
    int Age,
    string? MiddleName,
    AddressModel? HomeAddress);

public sealed record AddressModel(string? Street, string? City);
