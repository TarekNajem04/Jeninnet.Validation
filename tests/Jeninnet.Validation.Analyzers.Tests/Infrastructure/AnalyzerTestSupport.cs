//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Analyzers.Tests.Infrastructure;

/// <summary>
/// Shared compilation helpers for analyzer and code-fix tests.
/// </summary>
internal static class AnalyzerTestSupport {
    internal static List<MetadataReference> CreateReferences() {
        var references = new List<MetadataReference> {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(int).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Jeninnet.Validation.AsyncOnlyValidatorAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Jeninnet.Validation.Attributes.ValidateAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Jeninnet.Validation.DataAnnotations.JeninnetValidatableAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute).Assembly.Location)
        };

        if(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssemblies) {
            var needed = new[] { "System.Runtime", "System.Collections", "mscorlib", "System.Private.CoreLib" };

            foreach(var path in trustedAssemblies.Split(Path.PathSeparator)) {
                var name = Path.GetFileNameWithoutExtension(path);
                if(needed.Contains(name)) {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        return references;
    }

    internal static CSharpCompilation CreateCompilation(string source, CancellationToken cancellationToken) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            CreateReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }

    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        CancellationToken cancellationToken) {
        var compilation = CreateCompilation(source, cancellationToken);
        var withAnalyzers = compilation.WithAnalyzers([analyzer]);

        return await withAnalyzers.GetAllDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }
}
