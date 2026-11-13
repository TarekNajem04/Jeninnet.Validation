//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

file sealed class Shipment {
    public string? Recipient { get; init; }
}

/// <summary>
/// Async-only: the single rule of this validator is asynchronous.
/// </summary>
file sealed class ShipmentValidator : Validator<Shipment> {
    protected override void Configure(ValidatorRuleBuilder<Shipment> builder) =>
        builder.RuleFor(static x => x.Recipient, nameof(Shipment.Recipient))
               .Must(static (recipient, _) => new ValueTask<bool>(recipient is { Length: > 0 }));
}

/// <summary>
/// Mixed: one sync rule + one async rule on the same member.
/// </summary>
file sealed class MixedShipmentValidator : Validator<Shipment> {
    protected override void Configure(ValidatorRuleBuilder<Shipment> builder) =>
        builder.RuleFor(static x => x.Recipient, nameof(Shipment.Recipient))
               .NotEmpty("Recipient is required.")
               .Must(static (recipient, _) => new ValueTask<bool>(recipient is { Length: > 3 }));
}

file sealed class ShipmentEnvelope {
    public string? Code { get; init; }
    public Shipment? Shipment { get; init; }
}

/// <summary>
/// Async-only content wrapped in a rule set.
/// </summary>
file sealed class AsyncRuleSetEnvelopeValidator : Validator<ShipmentEnvelope> {
    protected override void Configure(ValidatorRuleBuilder<ShipmentEnvelope> builder) =>
        builder.RuleSet("async", inner =>
            inner.RuleFor(static x => x.Code, nameof(ShipmentEnvelope.Code))
                 .Must(static (code, _) => new ValueTask<bool>(code is { Length: > 0 })));
}

/// <summary>
/// Async-only content wrapped in inline child rules.
/// </summary>
file sealed class AsyncChildRulesEnvelopeValidator : Validator<ShipmentEnvelope> {
    protected override void Configure(ValidatorRuleBuilder<ShipmentEnvelope> builder) =>
        builder.RuleFor(static x => x.Shipment, nameof(ShipmentEnvelope.Shipment))
               .ChildRules(inner =>
                   inner.RuleFor(static x => x.Recipient, nameof(Shipment.Recipient))
                        .Must(static (recipient, _) => new ValueTask<bool>(recipient is { Length: > 0 })));
}

// ---------------------------------------------------------------------------
// Strict-async guard (matrix row 24)
// ---------------------------------------------------------------------------

[TestClass]
public sealed class CompositeAsyncOnlyGuardTests {
    // -------------------------------------------------------------------------
    // Async-only content: sync execution must throw, never lie
    // -------------------------------------------------------------------------

    [TestMethod]
    public void AsyncOnlyValidator_SyncValidate_ThrowsInsteadOfSilentSuccess() {
        var validator = new ShipmentValidator();

        var act = () => validator.Validate(new Shipment());

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>()
            .Which.AsyncRuleCount.Should().Be(1, "the validator declares exactly one async rule"));
    }

    [TestMethod]
    public void AsyncOnlyValidator_SyncValidate_ThrowMessageNamesValidatorAndCount() {
        var validator = new ShipmentValidator();

        var act = () => validator.Validate(new Shipment());

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>()
            .Which.ValidatorTypeName.Should().Contain(nameof(ShipmentValidator), "the message must name the validator"));
        // Compiling the exception a second time exercises the cached sync-content scan.
        var secondAct = () => validator.Validate(new Shipment());
        MSTestAssertions.That(() => secondAct.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>());
    }

    [TestMethod]
    public void AsyncOnlyValidator_SyncValidateWithRuleSets_StillThrows() {
        var validator = new ShipmentValidator();

        var act = () => validator.Validate(new Shipment(), ["default"]);

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>());
    }

    [TestMethod]
    public async Task AsyncOnlyValidator_AsyncPath_EvaluatesTheRuleAsync() {
        var validator = new ShipmentValidator();
        var result = await validator.ValidateAsync(
            new Shipment { Recipient = "" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        ValidationAssert.HasError(result, nameof(Shipment.Recipient), "the async path must run the async rule");
    }

    // -------------------------------------------------------------------------
    // Async-only content hidden inside wrappers
    // -------------------------------------------------------------------------

    [TestMethod]
    public void AsyncOnlyRuleSet_SyncValidate_ThrowsInsteadOfSilentSuccess() {
        var validator = new AsyncRuleSetEnvelopeValidator();

        var act = () => validator.Validate(new ShipmentEnvelope { Code = string.Empty });

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>());
    }

    [TestMethod]
    public void AsyncOnlyChildRules_SyncValidate_ThrowsInsteadOfSilentSuccess() {
        var validator = new AsyncChildRulesEnvelopeValidator();

        var act = () => validator.Validate(new ShipmentEnvelope {
            Shipment = new Shipment { Recipient = string.Empty }
        });

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>());
    }

    // -------------------------------------------------------------------------
    // Mixed content: sync path keeps running the sync subset
    // -------------------------------------------------------------------------

    [TestMethod]
    public void MixedValidator_SyncValidate_RunsSyncSubset_Unchanged() {
        var validator = new MixedShipmentValidator();
        var result = validator.Validate(new Shipment { Recipient = "" });

        ValidationAssert.HasError(result, nameof(Shipment.Recipient), "the sync rule must still surface errors");
    }

    [TestMethod]
    public async Task MixedValidator_AsyncPath_RunsBothPathsAsync() {
        var validator = new MixedShipmentValidator();
        var result = await validator.ValidateAsync(
            new Shipment { Recipient = "abc" },
            TestContext?.CancellationToken ?? CancellationToken.None);

        // 'abc' passes the sync NotEmpty rule and fails the async length rule:
        // exactly one error from the async rule.
        ValidationAssert.ErrorCount(result, 1);
        ValidationAssert.HasError(result, nameof(Shipment.Recipient));
    }

    // -------------------------------------------------------------------------
    // Direct construction: the guard covers rules handed to the composite
    // -------------------------------------------------------------------------

    [TestMethod]
    public void DirectComposite_WithAsyncRulesOnly_ThrowsWithGivenName() {
        var composite = new CompositeValidator<Shipment>(
            asyncRules: [
                new MustAsyncRule<Shipment, string?>(
                    ValidatorFactory.Accessor<Shipment, string?>(static x => x.Recipient, nameof(Shipment.Recipient)),
                    static (recipient, _) => new ValueTask<bool>(recipient is { Length: > 0 }),
                    "Recipient must be filled."
                )
            ],
            validatorName: "CustomShipmentGateway");

        var act = () => composite.Validate(new Shipment());

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>()
            .Which.ValidatorTypeName.Should().Be("CustomShipmentGateway", "the caller-provided name wins"));
    }

    [TestMethod]
    public void DirectComposite_WithoutValidatorName_FallsBackToValidatedType() {
        var composite = new CompositeValidator<Shipment>(
            asyncRules: [
                new MustAsyncRule<Shipment, string?>(
                    ValidatorFactory.Accessor<Shipment, string?>(static x => x.Recipient, nameof(Shipment.Recipient)),
                    static (recipient, _) => new ValueTask<bool>(recipient is { Length: > 0 })
                )
            ]);

        var act = () => composite.Validate(new Shipment());

        MSTestAssertions.That(() => act.Should().ThrowExactly<AsyncValidatorInvokedSynchronouslyException>()
            .Which.ValidatorTypeName.Should().Contain(nameof(Shipment), "the fallback names the validated type"));
    }

    public TestContext? TestContext { get; set; }
}
