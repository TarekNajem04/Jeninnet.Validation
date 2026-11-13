//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Core;

/// <summary>
/// Verifies the <see cref="MessageId"/> behavior and equality.
/// </summary>
[TestClass]
public sealed class MessageIdTests {
    [TestMethod]
    public void MessageId_Constructor_SetsId() {
        var messageId = new MessageId("TestId");
        MSTestAssertions.That(() => messageId.Id.Should().Be("TestId"));
    }

    [TestMethod]
    public void MessageId_None_ReturnsEmptyId() =>
        MSTestAssertions.That(() => MessageId.None.Id.Should().BeEmpty());

    [TestMethod]
    public void MessageId_Invalid_ReturnsInvalidId() =>
        MSTestAssertions.That(() => MessageId.Invalid.Id.Should().Be("Invalid"));

    [TestMethod]
    public void MessageId_Equality_WorksCorrectly() {
        var id1 = new MessageId("A");
        var id2 = new MessageId("A");
        var id3 = new MessageId("B");

        MSTestAssertions.That(() => id1.Should().Be(id2));
        MSTestAssertions.That(() => id1.Should().NotBe(id3));
    }
}
