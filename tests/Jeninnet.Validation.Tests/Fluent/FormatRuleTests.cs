//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Fluent;

// ---------------------------------------------------------------------------
// Domain stubs
// ---------------------------------------------------------------------------

internal sealed class FormatModel {
    public string? Code { get; set; }
    public string? Payload { get; set; }
    public string? FileName { get; set; }
}

file sealed class ExactLengthValidator : Validator<FormatModel> {
    protected override void Configure(ValidatorRuleBuilder<FormatModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Code, nameof(FormatModel.Code))
               .ExactLength(5);
    }
}

file sealed class Base64Validator : Validator<FormatModel> {
    protected override void Configure(ValidatorRuleBuilder<FormatModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Payload, nameof(FormatModel.Payload))
               .Base64String();
    }
}

file sealed class FileExtensionsValidator : Validator<FormatModel> {
    protected override void Configure(ValidatorRuleBuilder<FormatModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.FileName, nameof(FormatModel.FileName))
               .FileExtensions(".pdf", "jpg");
    }
}

file sealed class NonStandardFileExtensionsValidator : Validator<FormatModel> {
    protected override void Configure(ValidatorRuleBuilder<FormatModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.FileName, nameof(FormatModel.FileName))
               .FileExtensions([".pdf"], allowNonStandard: true);
    }
}

file sealed class CaseSensitiveFileExtensionsValidator : Validator<FormatModel> {
    protected override void Configure(ValidatorRuleBuilder<FormatModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.FileName, nameof(FormatModel.FileName))
               .FileExtensions([".pdf"], ignoreCase: false);
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[TestClass]
public sealed class FormatRuleTests {
    [TestMethod]
    public void ExactLength_ValidatesAndExposesParameters() {
        var validator = new ExactLengthValidator();

        ValidationAssert.IsValid(validator.Validate(new FormatModel { Code = "12345" }));

        var invalid = validator.Validate(new FormatModel { Code = "1234" });
        ValidationAssert.ErrorCount(invalid, 1);
        MSTestAssertions.That(() => invalid.Errors[0].MessageKey.Should().Be(ValidationMessageKeys.EXACT_LENGTH));
        MSTestAssertions.That(() => invalid.Errors[0].MessageParameters!["Length"].Should().Be(5));
        MSTestAssertions.That(() => invalid.Errors[0].MessageParameters!["ExactLength"].Should().Be(5));

        ValidationAssert.ErrorCount(validator.Validate(new FormatModel { Code = "123456" }), 1);
    }

    [TestMethod]
    public void Base64String_AcceptsWhitespaceAndRejectsMalformedPayloads() {
        var validator = new Base64Validator();

        ValidationAssert.IsValid(validator.Validate(new FormatModel()));
        ValidationAssert.IsValid(validator.Validate(new FormatModel { Payload = string.Empty }));
        ValidationAssert.IsValid(validator.Validate(new FormatModel { Payload = "SGVsbG8=" }));
        ValidationAssert.IsValid(validator.Validate(new FormatModel { Payload = "SGVs bG8=" }));
        ValidationAssert.IsValid(validator.Validate(new FormatModel { Payload = "SGVs\nbG8=" }));

        ValidationAssert.HasError(validator.Validate(new FormatModel { Payload = "SGVsbG8" }), nameof(FormatModel.Payload));
        ValidationAssert.HasError(validator.Validate(new FormatModel { Payload = "not base64!" }), nameof(FormatModel.Payload));
        ValidationAssert.HasError(validator.Validate(new FormatModel { Payload = "A===" }), nameof(FormatModel.Payload));
        ValidationAssert.HasError(validator.Validate(new FormatModel { Payload = "AA=A" }), nameof(FormatModel.Payload));
    }

    [TestMethod]
    public void FileExtensions_MatchesCaseInsensitivelyAndExposesExtensions() {
        var validator = new FileExtensionsValidator();

        ValidationAssert.IsValid(validator.Validate(new FormatModel { FileName = "document.pdf" }));
        ValidationAssert.IsValid(validator.Validate(new FormatModel { FileName = "photo.JPG" }));

        var invalid = validator.Validate(new FormatModel { FileName = "archive.zip" });
        ValidationAssert.ErrorCount(invalid, 1);
        MSTestAssertions.That(() => invalid.Errors[0].MessageKey.Should().Be(ValidationMessageKeys.FILE_EXTENSIONS));
        MSTestAssertions.That(() => invalid.Errors[0].MessageParameters!["Extensions"].Should().Be(".pdf, .jpg"));

        ValidationAssert.HasError(validator.Validate(new FormatModel { FileName = "README" }), nameof(FormatModel.FileName));
    }

    [TestMethod]
    public void FileExtensions_AllowNonStandardAcceptsFilesWithoutExtension() {
        var validator = new NonStandardFileExtensionsValidator();

        ValidationAssert.IsValid(validator.Validate(new FormatModel { FileName = "README" }));
        ValidationAssert.IsValid(validator.Validate(new FormatModel { FileName = "document.pdf" }));
        ValidationAssert.HasError(validator.Validate(new FormatModel { FileName = "archive.zip" }), nameof(FormatModel.FileName));
    }

    [TestMethod]
    public void FileExtensions_CanBeCaseSensitive() {
        var validator = new CaseSensitiveFileExtensionsValidator();

        ValidationAssert.IsValid(validator.Validate(new FormatModel { FileName = "document.pdf" }));
        ValidationAssert.HasError(validator.Validate(new FormatModel { FileName = "document.PDF" }), nameof(FormatModel.FileName));
    }
}
