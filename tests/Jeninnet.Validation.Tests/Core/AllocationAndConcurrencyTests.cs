//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Core;

// ---------------------------------------------------------------------------
// Models and validators
// ---------------------------------------------------------------------------

internal sealed class AllocationModel {
    public string? Name { get; set; }
    public int Age { get; set; }
}

file sealed class AllocationModelValidator : Validator<AllocationModel> {
    protected override void Configure(ValidatorRuleBuilder<AllocationModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(AllocationModel.Name))
               .NotEmpty();

        builder.RuleFor(static x => x.Age, nameof(AllocationModel.Age))
               .InclusiveBetween(18, 99);
    }
}

internal sealed class NestedAllocationModel {
    public AllocationModel? Child { get; set; }
}

file sealed class NestedAllocationModelValidator : Validator<NestedAllocationModel> {
    protected override void Configure(ValidatorRuleBuilder<NestedAllocationModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Child, nameof(NestedAllocationModel.Child))
               .ChildRules(child =>
                   child.RuleFor(static x => x.Name, nameof(AllocationModel.Name))
                        .NotEmpty());
    }
}

internal sealed class AsyncAllocationModel {
    public string? Name { get; set; }
}

file sealed class AsyncAllocationModelValidator : Validator<AsyncAllocationModel> {
    protected override void Configure(ValidatorRuleBuilder<AsyncAllocationModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(AsyncAllocationModel.Name))
               .Must(static async (value, cancellationToken) => {
                   await Task.Yield();
                   cancellationToken.ThrowIfCancellationRequested();
                   return value == "ok";
               });
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class AllocationAndConcurrencyTests {
    [TestMethod]
    public void Validate_HappyPath_DoesNotAllocateAfterWarmup() {
        var validator = new AllocationModelValidator();
        var model = new AllocationModel { Name = "Valid", Age = 25 };

        for(var i = 0; i < 1_000; i++) {
            validator.Validate(model);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for(var i = 0; i < 1_000; i++) {
            validator.Validate(model);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
#if DEBUG
        // Debug codegen allocates the pooled collector per call; Release is zero.
        const int maximumAllocated = 32 * 1_000;
#else
        const int maximumAllocated = 0;
#endif
        MSTestAssertions.That(() => (allocated <= maximumAllocated).Should().BeTrue(
            $"the happy path should not allocate beyond the pooled budget, but allocated {allocated} bytes"));
    }

    [TestMethod]
    public void Validate_InvalidPathAcrossThreads_DoesNotLeakErrors() {
        var validator = new AllocationModelValidator();
        var invalid = new AllocationModel { Name = "", Age = 100 };
        var expected = validator.Validate(invalid).Errors.Count;
        var failures = 0;

        Parallel.For(0, 10_000, _ => {
            var result = validator.Validate(invalid);

            if(result.Errors.Count != expected) {
                Interlocked.Increment(ref failures);
            }
        });

        MSTestAssertions.That(() => expected.Should().Be(2));
        MSTestAssertions.That(() => failures.Should().Be(0));
    }

    [TestMethod]
    public void Validate_NestedValidators_RepeatedCallsAreStable() {
        var validator = new NestedAllocationModelValidator();
        var invalid = new NestedAllocationModel { Child = new AllocationModel { Name = "", Age = 25 } };
        var valid = new NestedAllocationModel { Child = new AllocationModel { Name = "ok", Age = 25 } };

        for(var i = 0; i < 1_000; i++) {
            MSTestAssertions.That(() => validator.Validate(invalid).Errors.Should().HaveCount(1));
            MSTestAssertions.That(() => validator.Validate(valid).IsValid.Should().BeTrue());
        }
    }

    [TestMethod]
    public async Task ValidateAsync_AfterContinuation_DoesNotLeakErrorsAsync() {
        var validator = new AsyncAllocationModelValidator();
        var invalid = new AsyncAllocationModel { Name = "bad" };
        var valid = new AsyncAllocationModel { Name = "ok" };

        for(var i = 0; i < 1_000; i++) {
            var invalidResult = await validator.ValidateAsync(invalid, TestCancellation);
            var validResult = await validator.ValidateAsync(valid, TestCancellation);

            MSTestAssertions.That(() => invalidResult.Errors.Should().HaveCount(1));
            MSTestAssertions.That(() => validResult.IsValid.Should().BeTrue());
        }
    }

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }
}
