//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Tests.Rules.String;

file sealed class Registration {
    public string? PostalCode { get; init; }
}

[TestClass]
public sealed class NetworkRuleTests {
    [TestMethod]
    [DataRow("127.0.0.1")]
    [DataRow("192.168.1.1")]
    [DataRow("2001:db8::1")]
    public void IPAddress_Valid_Passes(string ip) {
        var result = RunIP(ip);
        ValidationAssert.IsValid(result, $"'{ip}' should be a valid IP.");
    }

    [TestMethod]
    [DataRow("999.999.999.999")]
    [DataRow("not-an-ip")]
    public void IPAddress_Invalid_Fails(string ip) {
        var result = RunIP(ip);
        ValidationAssert.HasError(result, nameof(Registration.PostalCode)); // reusing model
    }

    [TestMethod]
    [DataRow("https://google.com")]
    [DataRow("http://localhost:8080")]
    [DataRow("ftp://files.example.org")]
    public void Url_Valid_Passes(string url) {
        var result = RunUrl(url);
        ValidationAssert.IsValid(result, $"'{url}' should be a valid URL.");
    }

    [TestMethod]
    [DataRow("just-a-string")]
    [DataRow("http://")]
    public void Url_Invalid_Fails(string url) {
        var result = RunUrl(url);
        ValidationAssert.HasError(result, nameof(Registration.PostalCode));
    }

    private static ValidationResult RunIP(string? value) {
        var rule = new IPAddressRule<Registration, string?>(
            ValidatorFactory.Accessor<Registration, string?>(x => x.PostalCode, nameof(Registration.PostalCode)),
            null);
        return RuleRunner.Run(rule, new Registration { PostalCode = value });
    }

    private static ValidationResult RunUrl(string? value) {
        var rule = new UrlRule<Registration, string?>(
            ValidatorFactory.Accessor<Registration, string?>(x => x.PostalCode, nameof(Registration.PostalCode)),
            null);
        return RuleRunner.Run(rule, new Registration { PostalCode = value });
    }
}
