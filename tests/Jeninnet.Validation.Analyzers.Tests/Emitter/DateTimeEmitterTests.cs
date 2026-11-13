//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Analyzers.Tests.Emitter;

[TestClass]
public class DateTimeEmitterTests {
    [TestMethod]
    public void DateTimeRuleEmitterTest() {
        const string source =
            """
            using System;
            using Jeninnet.Validation.Attributes;

            namespace TestNamespace;

            [Validate]
            public class Appointment
            {
                [After("2023-01-01")]
                public DateTime StartTime { get; set; }

                [Before("2023-12-31")]
                public DateTimeOffset EndTime { get; set; }

                [After("2023-01-01")]
                public DateOnly StartDate { get; set; }

                [Before("14:30")]
                public TimeOnly EndTimeOfDay { get; set; }

                [After("2023-01-01T00:00:00+02:00")]
                public DateTime OffsetDateTime { get; set; }
            }
            """;

        var generatedCode = GetGeneratedCode(source, "TestNamespace.Appointment");

        MSTestAssertions.That(() => generatedCode.Should().Contain(
            "new AfterRule<Appointment, System.DateTime>(ValidatorFactory.Accessor<Appointment, System.DateTime>(x => x.StartTime, \"StartTime\"), " +
            "global::System.DateTime.Parse(\"2023-01-01T00:00:00.0000000\", global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.RoundtripKind), null)"));

        MSTestAssertions.That(() => generatedCode.Should().Contain(
            "new BeforeRule<Appointment, System.DateTimeOffset>(ValidatorFactory.Accessor<Appointment, System.DateTimeOffset>(x => x.EndTime, \"EndTime\"), " +
            "global::System.DateTimeOffset.Parse(\"2023-12-31T00:00:00.0000000+00:00\", global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.AssumeUniversal), null)"));

        MSTestAssertions.That(() => generatedCode.Should().Contain(
            "new AfterRule<Appointment, System.DateOnly>(ValidatorFactory.Accessor<Appointment, System.DateOnly>(x => x.StartDate, \"StartDate\"), " +
            "global::System.DateOnly.Parse(\"2023-01-01\", global::System.Globalization.CultureInfo.InvariantCulture), null)"));

        MSTestAssertions.That(() => generatedCode.Should().Contain(
            "new BeforeRule<Appointment, System.TimeOnly>(ValidatorFactory.Accessor<Appointment, System.TimeOnly>(x => x.EndTimeOfDay, \"EndTimeOfDay\"), " +
            "global::System.TimeOnly.Parse(\"14:30:00.0000000\", global::System.Globalization.CultureInfo.InvariantCulture), null)"));

        MSTestAssertions.That(() => generatedCode.Should().Contain(
            "new AfterRule<Appointment, System.DateTime>(ValidatorFactory.Accessor<Appointment, System.DateTime>(x => x.OffsetDateTime, \"OffsetDateTime\"), " +
            "global::System.DateTime.Parse(\"2022-12-31T22:00:00.0000000Z\", global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.RoundtripKind), null)"));
    }

    private static string GetGeneratedCode(string source, string typeName) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attributes.ValidateAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "System.Runtime").Location),
            MetadataReference.CreateFromFile(typeof(DateTime).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DateTimeOffset).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var symbol = compilation.GetTypeByMetadataName(typeName) ?? throw new ArgumentException($"Type {typeName} not found in compilation");

        var descriptor = ValidatorMetadataBuilder.TryBuildValidatorDescriptor(symbol) ?? throw new InvalidOperationException("Failed to build ValidatorDescriptor");
        return ValidatorEmitter.Emit(descriptor);
    }
}
