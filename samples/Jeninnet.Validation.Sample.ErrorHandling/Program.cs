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

Console.WriteLine("=== ERROR HANDLING COOKBOOK SAMPLE ===");
Console.WriteLine("Demonstrates ValidationResult, ValidateAndThrow,");
Console.WriteLine("CustomState, Severity, ErrorCode, and multiple errors.");
Console.WriteLine();

var validator = new PaymentRequestValidator();

var badPayment = new PaymentRequest(
    CardNumber: "123",
    CardholderName: "A",
    Amount: 0m,
    Currency: "XYZ");

// =============================================================
// Part 1: ValidationResult — inspect errors
// =============================================================
Console.WriteLine("--- Part 1: ValidationResult ---");
Console.WriteLine("Errors are inspected programmatically from the result.");
Console.WriteLine();

var result = await validator.ValidateAsync(badPayment);

Console.WriteLine($"IsValid: {result.IsValid}");
Console.WriteLine($"Total errors: {result.Errors.Count}");
Console.WriteLine();

foreach (var err in result.Errors)
{
    Console.WriteLine($"  [{err.MemberName}]");
    Console.WriteLine($"    Message:    {err.Message}");
    Console.WriteLine($"    Code:       {err.ErrorCode}");
    Console.WriteLine($"    Severity:   {err.Severity}");
    Console.WriteLine($"    CustomState:{FormatState(err.CustomState)}");
    Console.WriteLine();
}

// =============================================================
// Part 2: ValidateAndThrow — exception on failure
// =============================================================
Console.WriteLine("--- Part 2: ValidateAndThrow ---");
Console.WriteLine("Throws ValidationException when validation fails.");
Console.WriteLine();

try
{
    await validator.ValidateAndThrowAsync(badPayment);
    Console.WriteLine("  (No exception — valid)");
}
catch (ValidationException vex)
{
    Console.WriteLine($"  Caught ValidationException — {vex.Errors.Count} error(s)");
    foreach (var err in vex.Errors)
        Console.WriteLine($"    [{err.MemberName}] {err.Severity}: {err.Message}");
}

Console.WriteLine();

// =============================================================
// Part 3: Error severity demonstration
// =============================================================
Console.WriteLine("--- Part 3: Severity Levels ---");
Console.WriteLine("Errors can be Info, Warning, or Error.");
Console.WriteLine();

var errors = result.Errors;
Console.WriteLine($"  Info:    {errors.Count(e => e.Severity == ValidationErrorSeverity.Info)}");
Console.WriteLine($"  Warning: {errors.Count(e => e.Severity == ValidationErrorSeverity.Warning)}");
Console.WriteLine($"  Error:   {errors.Count(e => e.Severity == ValidationErrorSeverity.Error)}");
Console.WriteLine();

// =============================================================
// Part 4: Successful validation
// =============================================================
Console.WriteLine("--- Part 4: Successful Validation ---");
Console.WriteLine("No errors, no exception.");
Console.WriteLine();

var validPayment = new PaymentRequest(
    CardNumber: "4111111111111111",
    CardholderName: "John Doe",
    Amount: 99.95m,
    Currency: "USD");

try
{
    await validator.ValidateAndThrowAsync(validPayment);
    Console.WriteLine("  ValidateAndThrowAsync completed (no exception).");
    var goodResult = await validator.ValidateAsync(validPayment);
    Console.WriteLine($"  IsValid: {goodResult.IsValid}");
}
catch (ValidationException)
{
    Console.WriteLine("  Unexpected validation failure.");
}

Console.WriteLine();
Console.WriteLine("Done.");

static string FormatState(object? state)
{
    if (state is null) return " (none)";
    // Simple formatting for anonymous types
    var str = state.ToString();
    return str is not null && str != state.GetType().ToString() ? $" {str}" : $" {state}";
}
