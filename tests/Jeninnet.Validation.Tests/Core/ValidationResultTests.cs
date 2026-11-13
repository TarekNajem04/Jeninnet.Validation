//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

/// <summary>
/// Verifies the <see cref="ValidationResult"/> memory model.
/// </summary>
[TestClass]
public sealed class ValidationResultTests {
    // -------------------------------------------------------------------------
    // Success singleton
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Success_IsValid_IsTrue() =>
        MSTestAssertions.That(() => ValidationResult.Success.IsValid.Should().BeTrue());

    [TestMethod]
    public void Success_Errors_IsEmptyNotNull() {
        var errors = ValidationResult.Success.Errors;

        MSTestAssertions.That(() => errors.Should().NotBeNull("Errors must never be null."));
        MSTestAssertions.That(() => errors.Should().BeEmpty());
    }

    // -------------------------------------------------------------------------
    // Parameterless constructor
    // -------------------------------------------------------------------------

    [TestMethod]
    public void DefaultConstructor_ProducesValidResult() {
        var result = new ValidationResult();

        MSTestAssertions.That(() => result.IsValid.Should().BeTrue());
        MSTestAssertions.That(() => result.Errors.Should().NotBeNull());
        MSTestAssertions.That(() => result.Errors.Should().BeEmpty());
    }

    // -------------------------------------------------------------------------
    // Construction from collector
    // -------------------------------------------------------------------------

    [TestMethod]
    public void FromErrors_WithEmptyCollector_ReturnsSingleton() {
        var collector = new ValidationErrorCollector();
        var result = ValidationResult.FromErrors(collector);

        MSTestAssertions.That(() => result.IsValid.Should().BeTrue());
        MSTestAssertions.That(() => result.Errors.Should().BeEmpty());
    }

    [TestMethod]
    public void FromErrors_WithErrors_ReturnsInvalidResult() {
        var collector = new ValidationErrorCollector {
            { "Name", "Name is required." }
        };

        var result = ValidationResult.FromErrors(collector);

        MSTestAssertions.That(() => result.IsValid.Should().BeFalse());
        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Name"));
    }

    // -------------------------------------------------------------------------
    // Errors are frozen at construction time
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Errors_AreImmutableAfterConstruction() {
        // Arrange: build a result, then add more errors to the original collector.
        var collector = new ValidationErrorCollector {
            { "Email", "Invalid email." }
        };

        var result = ValidationResult.FromErrors(collector);

        // Act: mutate the collector after the result was produced.
        collector.Add("Name", "Name is required.");

        // Assert: the result must not reflect the post-construction addition.
        MSTestAssertions.That(() => result.Errors.Should().HaveCount(1,
            "ValidationResult.Errors must not be affected by mutations to the " +
            "ValidationErrorCollector after FromErrors() was called."));
    }

    [TestMethod]
    public void Errors_DoNotExposeTheBackingArray() {
        var collector = new ValidationErrorCollector {
            { "Name", "Name is required." }
        };

        var result = ValidationResult.FromErrors(collector);
        var backingArray = result.Errors as ValidationError[];

        MSTestAssertions.That(() => backingArray.Should().BeNull(
            "ValidationResult.Errors must expose a read-only view, not the backing array."));

        backingArray?.SetValue(new ValidationError("Tampered", new MessageId("Tampered"), "Tampered"), 0);

        MSTestAssertions.That(() => result.Errors[0].MemberName.Should().Be("Name",
            "a caller must not be able to mutate a validation result through a cast."));
    }

    [TestMethod]
    public void StructCopy_DoesNotShareMutableState() {
        // Arrange: two copies of the same result struct.
        var collector = new ValidationErrorCollector {
            { "Field", "Error." }
        };

        var copy1 = ValidationResult.FromErrors(collector);
        var copy2 = copy1;   // struct copy

        // Assert: both copies see the same (frozen) errors and neither can
        // be mutated through the other.
        MSTestAssertions.That(() => copy2.Errors.Should().HaveCount(copy1.Errors.Count));
        MSTestAssertions.That(() => ReferenceEquals(copy1.Errors, copy2.Errors).Should().BeTrue(
            "Both copies should reference the same immutable list — no extra allocation."));
    }

    // -------------------------------------------------------------------------
    // Null guard
    // -------------------------------------------------------------------------

    [TestMethod]
    public void FromErrors_WithNullCollector_Throws() =>
        MSTestAssertions.That(() => Invoking(static () => ValidationResult.FromErrors(null!)).Should().Throw<ArgumentNullException>());
}

/// <summary>
/// Verifies <see cref="ValidationErrorCollector"/> snapshot semantics.
/// </summary>
[TestClass]
public sealed class ValidationErrorCollectorTests {
    [TestMethod]
    public void Add_FirstCall_AllocatesInternalList() {
        var collector = new ValidationErrorCollector();

        MSTestAssertions.That(() => collector.Count.Should().Be(0));
        collector.Add("X", "msg");
        MSTestAssertions.That(() => collector.Count.Should().Be(1));
    }

    [TestMethod]
    public void HasErrors_ReturnsFalse_BeforeAnyAdd() =>
        MSTestAssertions.That(() => new ValidationErrorCollector().HasErrors().Should().BeFalse());

    [TestMethod]
    public void HasErrors_ReturnsTrue_AfterAdd() {
        var c = new ValidationErrorCollector {
            { "P", "m" }
        };
        MSTestAssertions.That(() => c.HasErrors().Should().BeTrue());
    }

    [TestMethod]
    public void ToList_ReturnsIndependentSnapshot() {
        var c = new ValidationErrorCollector {
            { "A", "1" }
        };

        var snapshot = c.ToList();

        // Mutate the collector after the snapshot.
        c.Add("B", "2");

        // The snapshot must be unaffected.
        MSTestAssertions.That(() => snapshot.Should().HaveCount(1,
            "ToList() must return a copy, not a live view."));
    }

    [TestMethod]
    public void ToList_OnEmptyCollector_ReturnsEmptyList() {
        var list = new ValidationErrorCollector().ToList();

        MSTestAssertions.That(() => list.Should().NotBeNull());
        MSTestAssertions.That(() => list.Should().BeEmpty());
    }

    [TestMethod]
    public void AddRange_FromOtherCollector_CopiesAllErrors() {
        var source = new ValidationErrorCollector {
            { "A", "1" },
            { "B", "2" }
        };

        var dest = new ValidationErrorCollector();
        dest.AddRange(source);

        MSTestAssertions.That(() => dest.Count.Should().Be(2));
    }

    [TestMethod]
    public void AddRange_FromNullCollector_DoesNotThrow() {
        var dest = new ValidationErrorCollector();
        dest.AddRange(null!);
        MSTestAssertions.That(() => dest.Count.Should().Be(0));
    }

    [TestMethod]
    public void Indexer_ReturnsCorrectError() {
        var c = new ValidationErrorCollector {
            { "Name", new MessageId("MessageId"), "Required", "ErrorCode"}
        };

        var error = c[0];

        MSTestAssertions.That(() => error.MemberName.Should().Be("Name"));
        MSTestAssertions.That(() => error.MessageId.Id.Should().Be("MessageId"));
        MSTestAssertions.That(() => error.Message.Should().Be("Required"));
        MSTestAssertions.That(() => error.ErrorCode.Should().Be("ErrorCode"));
        MSTestAssertions.That(() => error.Severity.Should().Be(ValidationErrorSeverity.Error));
    }
}
