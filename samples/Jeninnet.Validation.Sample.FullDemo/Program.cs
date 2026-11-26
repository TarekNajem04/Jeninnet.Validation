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

Console.WriteLine("=== JENINNET.VALIDATION FULL DEMO ===");
Console.WriteLine("Reference sample demonstrating every major feature.");
Console.WriteLine();

// ---- Setup localization with in-memory provider ----
// For file-based localization, see AddJsonLocalizationProviders() or AddXmlLocalizationProviders().
var services = new ServiceCollection();
services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(services);
services.AddSingleton<IInnerMessageProvider>(
    new MemoryMessageProvider(new MemoryLocalizationOptions
    {
        Messages = new Dictionary<string, string>
        {
            ["NotEmpty"] = "{PropertyName} must not be empty.",
            ["Email"] = "{PropertyName} must be a valid email address.",
            ["Length"] = "{PropertyName} has an invalid length.",
            ["InclusiveBetween"] = "{PropertyName} must be between {From} and {To}.",
            ["GreaterThan"] = "{PropertyName} must be greater than {ComparisonValue}.",
            ["Matches"] = "{PropertyName} is not in the correct format.",
        }
    }));
var sp = services.BuildServiceProvider();
var localizer = sp.GetRequiredService<IMessageLocalizer>();

async Task<string> LocalizeErrorAsync(ValidationError err, CultureInfo culture)
{
    var msg = err.Message;
    if (err.MessageKey is not null)
    {
        var parameters = err.MessageParameters?.ToDictionary(
            static kv => kv.Key, static kv => (object?)kv.Value) ?? [];
        parameters["PropertyName"] = err.MemberName;
        if(parameters.TryGetValue("0", out var p0)) {
            parameters.TryAdd("ComparisonValue", p0);
            parameters.TryAdd("From", p0);
            parameters.TryAdd("MinLength", p0);
        }
        if(parameters.TryGetValue("1", out var p1)) {
            parameters.TryAdd("To", p1);
            parameters.TryAdd("MaxLength", p1);
        }
        var localized = await localizer.LocalizeAsync(err.MessageKey, parameters, culture);
        if (!string.IsNullOrEmpty(localized))
            msg = localized;
    }
    return msg ?? "(no message)";
}

// ---- Create invalid order ----
var badOrder = new Order(
    OrderNumber: "BAD",
    Customer: new Customer(
        Name: "",
        Email: "not-email",
        Address: new Address("", "", "")),
    Lines:
    [
        new OrderLine("", 0, -1m),
    ],
    Discount: 120m);

// ---- Part 1: RuleSet + Nested Validation + All features ----
Console.WriteLine("--- 1. Validation with RuleSets ---");
Console.WriteLine();

var validator = new OrderValidator();

var defaultResult = await validator.ValidateAsync(badOrder, ruleSets: ["Default"]);
Console.WriteLine($"Default RuleSet — Errors: {defaultResult.Errors.Count}, Valid: {defaultResult.IsValid}");
Console.WriteLine();

foreach (var err in defaultResult.Errors)
{
    var msg = await LocalizeErrorAsync(err, new CultureInfo("en-US"));
    Console.WriteLine($"  [{err.MemberName}]");
    Console.WriteLine($"    Message:  {msg}");
    Console.WriteLine($"    Code:     {err.ErrorCode}");
    Console.WriteLine($"    Severity: {err.Severity}");
    if (err.CustomState is not null)
        Console.WriteLine($"    State:    {err.CustomState}");
    Console.WriteLine();
}

// ---- Part 2: ValidateAndThrow ----
Console.WriteLine("--- 2. ValidateAndThrow ---");
Console.WriteLine();

try
{
    await validator.ValidateAndThrowAsync(badOrder, ruleSets: ["Default"]);
    Console.WriteLine("  (No exception — unexpected)");
}
catch (ValidationException vex)
{
    Console.WriteLine($"  ValidationException thrown with {vex.Errors.Count} error(s).");
    Console.WriteLine();
    foreach (var err in vex.Errors.Take(3))
        Console.WriteLine($"    [{err.MemberName}] {err.Severity}: {err.ErrorCode}");
    if (vex.Errors.Count > 3)
        Console.WriteLine($"    ... and {vex.Errors.Count - 3} more.");
}

Console.WriteLine();

// ---- Part 3: Premium RuleSet ----
Console.WriteLine("--- 3. Premium RuleSet (adds extra rules) ---");
Console.WriteLine();

var premiumResult = await validator.ValidateAsync(badOrder,
    ruleSets: ["Default", "Premium"]);
Console.WriteLine($"Default + Premium — Errors: {premiumResult.Errors.Count}");
foreach (var err in premiumResult.Errors.Where(e => e.ErrorCode == "PREMIUM_MIN_LINES"))
    Console.WriteLine($"  [{err.MemberName}] {err.Message}");

Console.WriteLine();

// ---- Part 4: Severity breakdown ----
Console.WriteLine("--- 4. Severity Breakdown ---");
Console.WriteLine();

foreach (var severity in new[] {
    ValidationErrorSeverity.Info, ValidationErrorSeverity.Warning, ValidationErrorSeverity.Error })
{
    var count = defaultResult.Errors.Count(e => e.Severity == severity);
    Console.WriteLine($"  {severity}: {count}");
}

Console.WriteLine();

// ---- Part 5: Successful validation ----
Console.WriteLine("--- 5. Valid Order (all rules pass) ---");
Console.WriteLine();

var validOrder = new Order(
    OrderNumber: "ORD-123456",
    Customer: new Customer(
        Name: "Jane Doe",
        Email: "jane@example.com",
        Address: new Address("123 Main St", "New York", "USA")),
    Lines:
    [
        new OrderLine("Widget", 2, 19.99m),
        new OrderLine("Gadget", 1, 49.99m),
        new OrderLine("Extra", 3, 9.99m),
    ],
    Discount: 20m);

var validResult = await validator.ValidateAsync(validOrder,
    ruleSets: ["Default", "Premium"]);
Console.WriteLine($"  IsValid: {validResult.IsValid}");
Console.WriteLine($"  Errors:  {validResult.Errors.Count}");
foreach(var err in validResult.Errors)
    Console.WriteLine($"    [{err.MemberName}] {err.ErrorCode}: {err.Message}");

Console.WriteLine();

// ---- Part 6: Polymorphic inheritance validation ----
Console.WriteLine("--- 6. Inheritance Validation ---");
Console.WriteLine();

var paymentValidator = new PaymentEnvelopeValidator();
#pragma warning disable S6966 // This section deliberately demonstrates the synchronous overload.
var invalidCard = paymentValidator.Validate(
    new PaymentEnvelope(new CardPayment("CARD-1", "12")));
var invalidBank = paymentValidator.Validate(
    new PaymentEnvelope(new BankPayment("BANK-1", string.Empty)));
#pragma warning restore S6966

Console.WriteLine($"  Invalid card — Errors: {invalidCard.Errors.Count}");
foreach(var err in invalidCard.Errors)
    Console.WriteLine($"    [{err.MemberName}] {err.ErrorCode}: {err.Message}");
Console.WriteLine($"  Invalid bank — Errors: {invalidBank.Errors.Count}");
foreach(var err in invalidBank.Errors)
    Console.WriteLine($"    [{err.MemberName}] {err.ErrorCode}: {err.Message}");

Console.WriteLine();

// ---- Part 7: Filtered collection validation ----
Console.WriteLine("--- 7. Filtered Collection Validation ---");
Console.WriteLine();

var filteredValidator = new FilteredOrderValidator();
var filteredResult = await filteredValidator.ValidateAsync(validOrder with {
    Lines =
    [
        new OrderLine(string.Empty, 0, 1m),     // skipped: quantity filter
        new OrderLine(string.Empty, 1, 1m),     // validated: invalid product
        new OrderLine("Valid", 1, -1m)          // skipped: async price filter
    ]
});

Console.WriteLine($"  Errors after Where/WhereAsync: {filteredResult.Errors.Count}");
foreach(var err in filteredResult.Errors)
    Console.WriteLine($"    [{err.MemberName}] {err.ErrorCode}: {err.Message}");

Console.WriteLine();

// ---- Part 8: Reusable rule template ----
Console.WriteLine("--- 8. Reusable Rule Template ---");
Console.WriteLine();

var templateResult = await validator.ValidateAsync(
    validOrder with { OrderNumber = "TBD" },
    ruleSets: ["Default"]);
var templateErrors = templateResult.Errors
    .Where(static error => error.ErrorCode == "ORD_PLACEHOLDER")
    .ToArray();

Console.WriteLine($"  Template errors: {templateErrors.Length}");
foreach(var err in templateErrors)
    Console.WriteLine($"    [{err.MemberName}] {err.ErrorCode}: {err.Message}");

Console.WriteLine();
Console.WriteLine("=== FULL DEMO COMPLETE ===");

// =============================================================
// MODELS and VALIDATORS — declared at end of file
// =============================================================
#pragma warning disable S3903, RCS1110 // Sample types are intentionally file-local in this single-file demo
file sealed record Address(string Street, string City, string Country);
file sealed record Customer(string Name, string Email, Address? Address);
file sealed record OrderLine(string ProductName, int Quantity, decimal UnitPrice);
file sealed record Order(
    string OrderNumber,
    Customer Customer,
    List<OrderLine> Lines,
    decimal Discount);

file abstract record PaymentMethod(string Reference);
file sealed record CardPayment(string Reference, string Last4) : PaymentMethod(Reference);
file sealed record BankPayment(string Reference, string Iban) : PaymentMethod(Reference);
file sealed record PaymentEnvelope(PaymentMethod? Payment);

file sealed class AddressValidator : Validator<Address>
{
    protected override void Configure(ValidatorRuleBuilder<Address> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static a => a.Street, "Street").NotEmpty().Length(3, 100);
        builder.RuleFor(static a => a.City, "City").NotEmpty().Length(2, 50);
        builder.RuleFor(static a => a.Country, "Country").NotEmpty().Length(2, 60);
    }
}

file sealed class OrderLineValidator : Validator<OrderLine>
{
    protected override void Configure(ValidatorRuleBuilder<OrderLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RuleFor(static l => l.ProductName, "ProductName")
            .NotEmpty().Length(2, 200);
        builder.RuleFor(static l => l.Quantity, "Quantity")
            .GreaterThan(0).WithCode("QTY_POSITIVE");
        builder.RuleFor(static l => l.UnitPrice, "UnitPrice")
            .GreaterThan(0m).WithCode("PRICE_POSITIVE");
    }
}

file sealed class OrderValidator : Validator<Order>
{
    private static readonly RuleTemplate<string?> s_orderNumberTemplate = new(
        "OrderNumberNotPlaceholder",
        static value => !string.Equals(value, "TBD", StringComparison.OrdinalIgnoreCase),
        "Order number must be a real value, not a placeholder.",
        errorCode: "ORD_PLACEHOLDER");

    protected override void Configure(ValidatorRuleBuilder<Order> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleSet("Default", rules =>
        {
            rules.RuleFor(static o => o.OrderNumber, "OrderNumber")
                .NotEmpty().WithCode("ORD_REQUIRED")
                    .WithSeverity(ValidationErrorSeverity.Error)
                .Length(5, 20).WithCode("ORD_LENGTH")
                    .WithSeverity(ValidationErrorSeverity.Error)
                .Rule(s_orderNumberTemplate);

            rules.Include(extra =>
            {
                extra.RuleFor(static o => o.OrderNumber, "OrderNumber")
                    .Matches("^ORD-").WithCode("ORD_PREFIX")
                    .WithState(new { ExpectedPrefix = "ORD-" });
            });

            rules.RuleFor(static o => o.Customer, "Customer")
                .ChildRules(customer =>
                {
                    customer.RuleFor(static c => c.Name, "Name").NotEmpty()
                        .WithCode("CUST_NAME").WithSeverity(ValidationErrorSeverity.Error);
                    customer.RuleFor(static c => c.Email, "Email").NotEmpty().EmailAddress()
                        .WithCode("CUST_EMAIL");

                    customer.RuleFor(static c => c.Address, "Address")
                        .SetValidator(new AddressValidator());
                });

            rules.RuleFor(static o => o.Lines, "Lines")
                .NotEmpty().WithCode("LINES_REQUIRED")
                .ForEach(new OrderLineValidator());

            rules.RuleFor(static o => o.Discount, "Discount")
                .InclusiveBetween(0m, 100m).WithCode("DISCOUNT_RANGE")
                    .WithSeverity(ValidationErrorSeverity.Error)
                .GreaterThan(15m).WithCode("DISCOUNT_HIGH")
                    .WithSeverity(ValidationErrorSeverity.Info)
                    .WithState(new { Suggestion = "Discounts over 15% require manager approval." });
        });

        builder.RuleSet("Premium", rules =>
        {
            rules.RuleFor(static o => o.Lines, "Lines")
                .Must(lines => lines?.Count >= 3)
                .WithCode("PREMIUM_MIN_LINES")
                .WithMessage("Premium orders require at least 3 line items.");
        });
    }
}

file sealed class AsyncOrderLineValidator : Validator<OrderLine>
{
    protected override void Configure(ValidatorRuleBuilder<OrderLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static l => l.ProductName, "ProductName")
            .NotEmpty().Length(2, 200);
        builder.RuleFor(static l => l.Quantity, "Quantity")
            .GreaterThan(0).WithCode("QTY_POSITIVE");
        builder.RuleFor(static l => l.UnitPrice, "UnitPrice")
            .GreaterThan(0m).WithCode("PRICE_POSITIVE");
    }
}

file sealed class FilteredOrderValidator : Validator<Order>
{
    protected override void Configure(ValidatorRuleBuilder<Order> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static o => o.Lines, "Lines")
            .ForEach((IAsyncValidator<OrderLine>)new AsyncOrderLineValidator())
            .Where(static line => line.Quantity > 0)
            .WhereAsync(static (line, _) => new ValueTask<bool>(line.UnitPrice > 0m));
    }
}

file sealed class CardPaymentValidator : Validator<CardPayment>
{
    protected override void Configure(ValidatorRuleBuilder<CardPayment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static payment => payment.Last4, "Last4")
            .NotEmpty()
            .Length(4, 4)
            .WithCode("CARD_LAST4");
    }
}

file sealed class BankPaymentValidator : Validator<BankPayment>
{
    protected override void Configure(ValidatorRuleBuilder<BankPayment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static payment => payment.Iban, "Iban")
            .NotEmpty()
            .Length(15, 34)
            .WithCode("BANK_IBAN");
    }
}

file sealed class PaymentEnvelopeValidator : Validator<PaymentEnvelope>
{
    protected override void Configure(ValidatorRuleBuilder<PaymentEnvelope> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static envelope => envelope.Payment, "Payment")
            .SetInheritanceValidator(static map => map
                .Add<CardPayment>(new CardPaymentValidator())
                .Add<BankPayment>(new BankPaymentValidator()));
    }
}
#pragma warning restore S3903, RCS1110
