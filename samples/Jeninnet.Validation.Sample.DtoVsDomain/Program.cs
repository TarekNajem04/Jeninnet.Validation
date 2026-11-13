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

Console.WriteLine("=== DTO vs DOMAIN SAMPLE ===");
Console.WriteLine("Demonstrates separation of concerns between API DTOs,");
Console.WriteLine("domain entities, and validation logic.");
Console.WriteLine();

// =============================================================
// Part 1: DTO validation — input validation at the API layer
// =============================================================
Console.WriteLine("--- Part 1: DTO Validation (API Layer) ---");
Console.WriteLine("CreateUserDto is validated before reaching the domain.");
Console.WriteLine();

var createValidator = new CreateUserDtoValidator();

var invalidDto = new CreateUserDto(
    Username: "ab",          // too short
    Email: "not-an-email",   // invalid format
    Password: "short",       // too short
    ConfirmPassword: "",     // empty
    Age: 10);                 // under 13

var dtoResult = await createValidator.ValidateAsync(invalidDto);

Console.WriteLine("DTO validation errors:");
foreach (var err in dtoResult.Errors)
    Console.WriteLine($"  [{err.MemberName}] {err.Message}");

Console.WriteLine($"  Valid: {dtoResult.IsValid}");
Console.WriteLine();

// =============================================================
// Part 2: Domain entities have NO validation
// =============================================================
Console.WriteLine("--- Part 2: Domain Entity (No Validation) ---");
Console.WriteLine("The User entity is a pure data record with no validation.");
Console.WriteLine("It only becomes 'valid' after passing DTO validation.");
Console.WriteLine();

var user = new User(
    Id: Guid.NewGuid().ToString(),
    Username: "johndoe",
    Email: "john@example.com",
    PasswordHash: "HASHED_VALUE",
    Age: 30,
    CreatedAt: DateTime.UtcNow);

Console.WriteLine($"  User created: {user.Username}, {user.Email}, Age {user.Age}");
Console.WriteLine("  (No validation on the domain entity itself.)");
Console.WriteLine();

// =============================================================
// Part 3: Valid DTO → Domain mapping
// =============================================================
Console.WriteLine("--- Part 3: Valid DTO → Domain Mapping ---");
Console.WriteLine("Only pass validated DTO data to the domain layer.");
Console.WriteLine();

var validDto = new CreateUserDto(
    Username: "johndoe",
    Email: "john@example.com",
    Password: "Str0ng!Pass",
    ConfirmPassword: "Str0ng!Pass",
    Age: 30);

var validResult = await createValidator.ValidateAsync(validDto);

if (validResult.IsValid)
{
    // Map DTO to domain entity (simulated)
    var newUser = new User(
        Id: Guid.NewGuid().ToString(),
        Username: validDto.Username,
        Email: validDto.Email,
        PasswordHash: "BCRYPT_HASH",
        Age: validDto.Age,
        CreatedAt: DateTime.UtcNow);

    Console.WriteLine($"  Domain User created: Id={newUser.Id}");
    Console.WriteLine($"  Username={newUser.Username}, Email={newUser.Email}");
}
else
{
    Console.WriteLine("  DTO validation FAILED — domain entity not created.");
}

Console.WriteLine();

// =============================================================
// Part 4: Where validators belong
// =============================================================
Console.WriteLine("--- Part 4: Separation of Concerns ---");
Console.WriteLine("  API Layer:     DTOs carry input — validate here first.");
Console.WriteLine("  Application:   Validators live here as separate classes.");
Console.WriteLine("  Domain:        Pure entities — no validation logic.");
Console.WriteLine("  Validators:    Testable, reusable, single-responsibility.");
Console.WriteLine();

Console.WriteLine("Done.");
