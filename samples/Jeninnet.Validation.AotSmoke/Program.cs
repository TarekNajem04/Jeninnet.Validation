//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

var failures = new List<string>();

async Task CheckAsync(string name, bool condition) {
    if(condition) {
        await Console.Out.WriteLineAsync($"PASS {name}");
    } else {
        failures.Add(name);
        await Console.Error.WriteLineAsync($"FAIL {name}");
    }
}

var services = new ServiceCollection();

services.AddSingleton(new LocalizationOptions { DefaultCulture = "en" });
JeninnetLocalizationExtensions.AddLocalization(services);
services.AddSingleton<IInnerMessageProvider>(
    new CultureAwareMessageProvider(
        new Dictionary<string, IMessageProvider> {
            ["en"] = new MemoryMessageProvider(
                new MemoryLocalizationOptions {
                    Messages = new Dictionary<string, string> {
                        [ValidationMessageKeys.NOT_EMPTY] = "{PropertyName} must not be empty.",
                        [ValidationMessageKeys.MUST] = "{PropertyName} is invalid.",
                    },
                }),
        }));

// One-call registration: the generated module initializer fills the catalog with typeof pairs,
// so AddValidation() registers every generated validator without assembly scanning.
services.AddValidation();

// Fluent validators are not generated from [Validate]; register them explicitly.
services.AddScoped<ShipmentValidator>();
services.AddScoped<IValidator<Shipment>, ShipmentValidator>();
services.AddScoped<AsyncOnlyRefundValidator>();
services.AddScoped<IValidator<AsyncOnlyRefund>, AsyncOnlyRefundValidator>();

var provider = services.BuildServiceProvider();

var orderValidator = provider.GetRequiredService<IValidator<Order>>();
var order = new Order { Reference = "ORD-1", Email = "not-an-email", Quantity = 0 };
var orderResult = orderValidator.Validate(order);

await CheckAsync("generated validator executes", !orderResult.IsValid);
await CheckAsync("generated email rule executes", orderResult.Errors.Any(static e => e.MemberName == nameof(Order.Email)));
await CheckAsync("generated range rule executes", orderResult.Errors.Any(static e => e.MemberName == nameof(Order.Quantity)));
await CheckAsync("custom rule executes", orderResult.Errors.Any(static e =>
    e.MemberName == nameof(Order.Reference) &&
    e.Message?.Contains("digit", StringComparison.OrdinalIgnoreCase) == true));

var flagsValidator = provider.GetRequiredService<IValidator<FlagsOrder>>();
var flagsValid = flagsValidator.Validate(new FlagsOrder { Access = OrderAccess.Read | OrderAccess.Write });
await CheckAsync("generated flags enum combination passes", flagsValid.IsValid);

var flagsInvalid = flagsValidator.Validate(new FlagsOrder { Access = (OrderAccess)8 });
await CheckAsync("generated flags enum undefined bit fails",
    !flagsInvalid.IsValid && flagsInvalid.Errors.Any(static e => e.MemberName == nameof(FlagsOrder.Access)));

var orderDescriptor = ((IValidationDescriptorProvider)orderValidator).Describe();
await CheckAsync("generated descriptor executes", orderDescriptor
    .RulesForMember(nameof(Order.Reference))
    .Any(static rule => rule.Kind == "Must"));

var strictResult = orderValidator.Validate(order, ["strict"]);
await CheckAsync("rule sets select rules", strictResult.IsValid);

var shipmentValidator = provider.GetRequiredService<IValidator<Shipment>>();
var shipmentResult = await shipmentValidator.ValidateAsync(new Shipment { Code = "" });
await CheckAsync("fluent async path executes", !shipmentResult.IsValid);

var refundValidator = provider.GetRequiredService<IValidator<AsyncOnlyRefund>>();
var guardThrew = false;
try {
    _ = refundValidator.Validate(new AsyncOnlyRefund { Reason = "no" });
}
catch(AsyncValidatorInvokedSynchronouslyException) {
    guardThrew = true;
}

await CheckAsync("strict-async guard throws", guardThrew);

var refundResult = await refundValidator.ValidateAsync(new AsyncOnlyRefund { Reason = "no" });
await CheckAsync("async-only rule executes", !refundResult.IsValid);

var localizer = provider.GetRequiredService<IMessageLocalizer>();
var localized = await localizer.LocalizeAsync(
    ValidationMessageKeys.NOT_EMPTY,
    new Dictionary<string, object?> { ["PropertyName"] = "Reference" },
    new CultureInfo("en"));
await CheckAsync("localization resolves", localized is { Length: > 0 });

var projections = orderResult.Errors
                             .Select(static error => new ErrorProjection(error.MemberName, error.Message))
                             .ToArray();
var json = JsonSerializer.Serialize(projections, SmokeJsonContext.Default.ErrorProjectionArray);
var roundTrip = JsonSerializer.Deserialize(json, SmokeJsonContext.Default.ErrorProjectionArray);
await CheckAsync("source-generated JSON round-trip", roundTrip is { Length: > 0 });

if(failures.Count > 0) {
    await Console.Error.WriteLineAsync($"AOT smoke failed: {failures.Count} check(s).");
    return 1;
}

await Console.Out.WriteLineAsync("AOT smoke passed.");
return 0;
