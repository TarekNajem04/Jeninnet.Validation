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
namespace Jeninnet.Validation.Sample.Authentication;

public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string ConfirmPassword,
    int Age);

public sealed record LoginRequest(string Username, string Password);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmNewPassword);
