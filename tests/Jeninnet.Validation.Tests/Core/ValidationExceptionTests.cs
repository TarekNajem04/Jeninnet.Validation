//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

[TestClass]
public sealed class ValidationExceptionTests {
    [TestMethod]
    public void DefaultConstructor_SetsDefaultMessage() {
        var ex = new ValidationException();
        MSTestAssertions.That(() => ex.Message.Should().Be("Validation failed."));
        MSTestAssertions.That(() => ex.Errors.Should().BeEmpty());
    }

    [TestMethod]
    public void ConstructorWithErrors_SetsErrorsAndMessage() {
        var errors = new List<ValidationError> { new("Name", "is invalid") };
        var ex = new ValidationException(errors);
        MSTestAssertions.That(() => ex.Errors.Should().HaveCount(1));
        MSTestAssertions.That(() => ex.Errors[0].MemberName.Should().Be("Name"));
        MSTestAssertions.That(() => ex.Message.Should().Contain("1 error(s)"));
    }

    [TestMethod]
    public void ConstructorWithErrors_ThrowsOnNull() =>
        MSTestAssertions.That(() => Invoking(static () => _ = new ValidationException(default(IReadOnlyList<ValidationError>)!)).Should().Throw<ArgumentNullException>());

    [TestMethod]
    public void ConstructorWithMessage_SetsGivenMessage() {
        var ex = new ValidationException("Custom error");
        MSTestAssertions.That(() => ex.Message.Should().Be("Custom error"));
        MSTestAssertions.That(() => ex.Errors.Should().BeEmpty());
    }

    [TestMethod]
    public void ConstructorWithMessageAndInnerException_SetsBoth() {
        var inner = new InvalidOperationException("inner");
        var ex = new ValidationException("outer", inner);
        MSTestAssertions.That(() => ex.Message.Should().Be("outer"));
        MSTestAssertions.That(() => ex.InnerException.Should().BeSameAs(inner));
        MSTestAssertions.That(() => ex.Errors.Should().BeEmpty());
    }
}
