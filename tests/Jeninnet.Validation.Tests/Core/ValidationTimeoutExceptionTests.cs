//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

[TestClass]
public sealed class ValidationTimeoutExceptionTests {
    [TestMethod]
    public void Constructors_PreserveMessageInnerAndCancellationToken() {
        var inner = new InvalidOperationException("inner");
        using var cancellation = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(2);

        var empty = new ValidationTimeoutException();
        var message = new ValidationTimeoutException("message");
        var messageAndInner = new ValidationTimeoutException("message", inner);
        var token = new ValidationTimeoutException(cancellation.Token);
        var messageAndToken = new ValidationTimeoutException("message", cancellation.Token);
        var messageInnerAndToken = new ValidationTimeoutException("message", inner, cancellation.Token);
        var deadline = new ValidationTimeoutException(timeout);
        var deadlineAndInner = new ValidationTimeoutException(timeout, inner);

        MSTestAssertions.That(() => empty.Message.Should().NotBeNullOrEmpty());
        MSTestAssertions.That(() => message.Message.Should().Be("message"));
        MSTestAssertions.That(() => ReferenceEquals(messageAndInner.InnerException, inner).Should().BeTrue());
        MSTestAssertions.That(() => token.CancellationToken.Should().Be(cancellation.Token));
        MSTestAssertions.That(() => messageAndToken.CancellationToken.Should().Be(cancellation.Token));
        MSTestAssertions.That(() => ReferenceEquals(messageInnerAndToken.InnerException, inner).Should().BeTrue());
        MSTestAssertions.That(() => messageInnerAndToken.CancellationToken.Should().Be(cancellation.Token));
        MSTestAssertions.That(() => deadline.Timeout.Should().Be(timeout));
        MSTestAssertions.That(() => deadlineAndInner.Timeout.Should().Be(timeout));
        MSTestAssertions.That(() => ReferenceEquals(deadlineAndInner.InnerException, inner).Should().BeTrue());
        MSTestAssertions.That(() => deadline.Message.Should().Contain(timeout.ToString()));
    }
}
