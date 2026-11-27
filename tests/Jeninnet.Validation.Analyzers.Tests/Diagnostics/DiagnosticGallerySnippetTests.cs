//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
using System.Text.Json;

namespace Jeninnet.Validation.Analyzers.Tests.Diagnostics;

/// <summary>
/// Compiles every fixed example in the public diagnostic gallery. The gallery generator and the
/// release gate prove that the pages match the data; this test proves that the "after" code is real
/// C# rather than documentation pseudocode.
/// </summary>
[TestClass]
public sealed class DiagnosticGallerySnippetTests {
    [TestMethod]
    public void EveryAvailableAfterExample_Compiles() {
        var repositoryRoot = FindRepositoryRoot();
        var galleryPath = Path.Combine(
            repositoryRoot,
            "docs",
            "en",
            "03-reference",
            "09-diagnostic-gallery",
            "diagnostics.json");

        MSTestAssertions.That(() => File.Exists(galleryPath).Should().BeTrue(
            $"the gallery data file was not found at '{galleryPath}'"));

        using var document = JsonDocument.Parse(File.ReadAllText(galleryPath));
        var failures = new List<string>();
        var compiled = 0;

        foreach(var entry in document.RootElement.GetProperty("diagnostics").EnumerateArray()) {
            var id = entry.GetProperty("id").GetString()!;

            if(entry.GetProperty("status").GetString() != "Available") {
                continue;
            }

            var after = JoinLines(entry.GetProperty("after"));
            var source = BuildSource(after);
            var compilation = AnalyzerTestSupport.CreateCompilation(source, TestCancellation);
            using var stream = new MemoryStream();
            var emit = compilation.Emit(stream, cancellationToken: TestCancellation);

            if(emit.Success) {
                compiled++;
                continue;
            }

            var errors = string.Join(
                Environment.NewLine,
                emit.Diagnostics
                    .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(static diagnostic => diagnostic.ToString()));

            failures.Add($"{id}:{Environment.NewLine}{errors}");
        }

        MSTestAssertions.That(() => failures.Should().BeEmpty(string.Join(Environment.NewLine, failures)));
        MSTestAssertions.That(() => compiled.Should().Be(11));
    }

    private static string BuildSource(string body) =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Jeninnet.Validation;
        using Jeninnet.Validation.Abstractions;
        using Jeninnet.Validation.Attributes;
        using Jeninnet.Validation.Attributes.Common;
        using Jeninnet.Validation.Attributes.String;
        using Jeninnet.Validation.Core;
        using Jeninnet.Validation.Fluent;
        using Jeninnet.Validation.Metadata;

        namespace DiagnosticGallery;

        {{body}}
        """;

    private static string JoinLines(JsonElement lines) =>
        string.Join(Environment.NewLine, lines.EnumerateArray().Select(static line => line.GetString()));

    private static string FindRepositoryRoot() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while(directory is not null) {
            if(File.Exists(Path.Combine(directory.FullName, "Jeninnet.Validation.slnx"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }
}
