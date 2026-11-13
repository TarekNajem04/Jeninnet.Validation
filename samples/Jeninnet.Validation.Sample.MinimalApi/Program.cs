//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable S6966

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<CreateUserValidator>();
var app = builder.Build();

app.MapPost("/users", (CreateUserRequest request, CreateUserValidator validator) => {
    var result = validator.Validate(request);
    if(!result.IsValid) {
        return Results.ValidationProblem(
            result.Errors
                  .GroupBy(e => e.MemberName)
                  .ToDictionary(g => g.Key, g => g.Select(e => e.Message ?? "Invalid").ToArray())
        );
    }

    return Results.Ok(new { Id = Guid.NewGuid() });
});

app.MapGet("/client-rules", (CreateUserValidator validator) =>
    Results.Text(ClientRulesWriter.WriteToString([validator]), "application/json"));

app.MapGet("/self-test", (CreateUserValidator validator) => {
    var results = new List<string>();

    var valid = new CreateUserRequest("jdoe", "j@example.com", 30, null, new AddressModel("Main St", "Amsterdam"));
    results.Add($"Valid: {validator.Validate(valid).IsValid} (expected: True)");

    var invalid = new CreateUserRequest("", "not-an-email", 15, null, new AddressModel("", ""));
    var r = validator.Validate(invalid);
    results.Add($"Invalid errors: {r.Errors.Count} (expected: 5)");

    foreach(var e in r.Errors) {
        results.Add($"  [{e.MemberName}] {e.Message} (code: {e.ErrorCode})");
    }

    try {
        validator.ValidateAndThrow(invalid);
    }
    catch(Jeninnet.Validation.ValidationException ex) {
        results.Add($"ValidateAndThrow caught: {ex.Errors.Count} error(s)");
    }

    return Results.Ok(results);
});

app.Run();
