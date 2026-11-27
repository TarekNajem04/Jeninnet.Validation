//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Tests.Diagnostics;

internal sealed class TraceModel {
    public string? Name { get; set; }

    public int Age { get; set; }
}

file sealed class TraceFluentValidator : Validator<TraceModel> {
    protected override void Configure(ValidatorRuleBuilder<TraceModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(TraceModel.Name))
               .NotEmpty();

        builder.RuleFor(static x => x.Age, nameof(TraceModel.Age))
               .InclusiveBetween(18, 99);
    }
}

file sealed class RuleSetTraceValidator : Validator<TraceModel> {
    protected override void Configure(ValidatorRuleBuilder<TraceModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(TraceModel.Name))
               .NotEmpty();

        builder.RuleFor(static x => x.Age, nameof(TraceModel.Age))
               .InclusiveBetween(18, 99)
               .RuleSet("create");
    }
}

file sealed class ConditionalTraceValidator : Validator<TraceModel> {
    protected override void Configure(ValidatorRuleBuilder<TraceModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.When(
            static x => x.Age >= 18,
            inner => inner.RuleFor(static x => x.Name, nameof(TraceModel.Name)).NotEmpty());
    }
}

file sealed class CascadeTraceValidator : Validator<TraceModel> {
    protected override void Configure(ValidatorRuleBuilder<TraceModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(TraceModel.Name))
               .NotEmpty()
               .EmailAddress()
               .Cascade(CascadeMode.Stop);
    }
}

file sealed class AsyncTraceValidator : Validator<TraceModel> {
    protected override void Configure(ValidatorRuleBuilder<TraceModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(TraceModel.Name))
               .Must(static async (value, cancellationToken) => {
                   await Task.Yield();
                   cancellationToken.ThrowIfCancellationRequested();
                   return !string.IsNullOrEmpty(value);
               });
    }
}

file sealed class CancellationTraceValidator : Validator<TraceModel> {
    protected override void Configure(ValidatorRuleBuilder<TraceModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(TraceModel.Name))
               .Must(static async (value, cancellationToken) => {
                   await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                   return value is not null;
               });
    }
}

file sealed class PrivacyTraceValidator : Validator<TraceModel> {
    protected override void Configure(ValidatorRuleBuilder<TraceModel> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RuleFor(static x => x.Name, nameof(TraceModel.Name))
               .EmailAddress()
               .WithMessage("SECRET MESSAGE");
    }
}

internal sealed class TraceCaptureSink : IValidationDiagnosticsSink {
    public List<ValidationRuleTraceEvent> Events { get; } = [];

    public IValidationDiagnosticsScope? Begin(
        Type modelType,
        string? validatorName,
        IEnumerable<string>? ruleSets,
        bool isAsync) {
        ArgumentNullException.ThrowIfNull(modelType);
        _ = validatorName;
        _ = ruleSets;
        _ = isAsync;

        return new Scope(this);
    }

    public void Reset() => Events.Clear();

    private sealed class Scope(TraceCaptureSink owner) : IValidationDiagnosticsScope {
        public bool IsRuleTraceEnabled => true;

        public void Rule(in ValidationRuleTraceEvent traceEvent) => owner.Events.Add(traceEvent);

        public void Complete(ValidationResult result) => _ = result;

        public void Fail(Exception exception) => _ = exception;

        public void Dispose() { }
    }
}

file sealed class DisabledTraceSink : IValidationDiagnosticsSink, IDisposable {
    private readonly Scope _scope = new();

    public IValidationDiagnosticsScope? Begin(
        Type modelType,
        string? validatorName,
        IEnumerable<string>? ruleSets,
        bool isAsync) {
        ArgumentNullException.ThrowIfNull(modelType);
        _ = validatorName;
        _ = ruleSets;
        _ = isAsync;

        return _scope;
    }

    public void Dispose() => _scope.Dispose();

    private sealed class Scope : IValidationDiagnosticsScope {
        public void Complete(ValidationResult result) => _ = result;

        public void Fail(Exception exception) => _ = exception;

        public void Dispose() { }
    }
}

[TestClass]
// Installs and restores the process-wide validation diagnostics sink.
[DoNotParallelize]
public sealed class RuleDecisionTraceTests {
    [TestMethod]
    public void FluentAndGeneratedShape_ProduceIdenticalEventSequences() {
        var sink = new TraceCaptureSink();
        ValidationDiagnostics.Sink = sink;

        try {
            new TraceFluentValidator().Validate(new TraceModel { Name = "Valid", Age = 25 });
            var fluent = Snapshot(sink);
            sink.Reset();

            CreateGeneratedShapeValidator().Validate(new TraceModel { Name = "Valid", Age = 25 });
            var generatedShape = Snapshot(sink);

            MSTestAssertions.That(() => generatedShape.Should().Be(fluent));
            MSTestAssertions.That(() => fluent.Should().Be(
                "NotEmpty@Name:Passed | InclusiveBetween@Age:Passed"));
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public void RuleSets_ReportOnlyTheSelectedRules() {
        var sink = new TraceCaptureSink();
        ValidationDiagnostics.Sink = sink;

        try {
            var validator = new RuleSetTraceValidator();
            var model = new TraceModel { Name = "Valid", Age = 25 };

            validator.Validate(model, new ValidationCallOptions { RuleSets = ["create"] });
            MSTestAssertions.That(() => Snapshot(sink).Should().Be("InclusiveBetween@Age:Passed"));

            sink.Reset();
            validator.Validate(model, new ValidationCallOptions { RuleSets = ["default"] });
            MSTestAssertions.That(() => Snapshot(sink).Should().Be("NotEmpty@Name:Passed"));
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public void ConditionalFalse_ReportsSkippedConditionWithoutRunningTheRule() {
        var sink = new TraceCaptureSink();
        ValidationDiagnostics.Sink = sink;

        try {
            _ = new ConditionalTraceValidator().Validate(new TraceModel { Name = string.Empty, Age = 10 });

            MSTestAssertions.That(() => Snapshot(sink).Should().Be("NotEmpty@Name:SkippedCondition"));
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public void CascadeStop_ReportsTheRemainingRuleAsShortCircuited() {
        var sink = new TraceCaptureSink();
        ValidationDiagnostics.Sink = sink;

        try {
            _ = new CascadeTraceValidator().Validate(new TraceModel { Name = string.Empty });

            MSTestAssertions.That(() => Snapshot(sink).Should().Be(
                "NotEmpty@Name:Failed | Email@Name:ShortCircuited"));
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public void FailFast_ReportsRemainingMembersAsShortCircuited() {
        var sink = new TraceCaptureSink();
        ValidationDiagnostics.Sink = sink;

        try {
            _ = new TraceFluentValidator().Validate(
                new TraceModel { Name = string.Empty, Age = 10 },
                new ValidationCallOptions { FailFast = true });

            MSTestAssertions.That(() => Snapshot(sink).Should().Be(
                "NotEmpty@Name:Failed | InclusiveBetween@Age:ShortCircuited"));
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public async Task AsyncRule_ReportsPassedDecisionAsync() {
        var sink = new TraceCaptureSink();
        ValidationDiagnostics.Sink = sink;

        try {
            var result = await new AsyncTraceValidator().ValidateAsync(
                new TraceModel { Name = "Valid" },
                TestCancellation);

            MSTestAssertions.That(() => result.IsValid.Should().BeTrue());
            MSTestAssertions.That(() => Snapshot(sink).Should().Be("MustAsync@Name:Passed"));
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public async Task CallerCancellation_IsNotMaskedByTheTraceAsync() {
        var sink = new TraceCaptureSink();
        ValidationDiagnostics.Sink = sink;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        try {
            var canceled = false;

            try {
                _ = await new CancellationTraceValidator().ValidateAsync(
                    new TraceModel { Name = "Valid" },
                    cancellation.Token);
            }
            catch(OperationCanceledException) {
                canceled = true;
            }

            MSTestAssertions.That(() => canceled.Should().BeTrue());
            MSTestAssertions.That(() => sink.Events.Should().BeEmpty());
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public void TraceEvent_CarriesNoPropertyValueOrMessage() {
        var sink = new TraceCaptureSink();
        ValidationDiagnostics.Sink = sink;

        try {
            _ = new PrivacyTraceValidator().Validate(new TraceModel { Name = "SECRET VALUE" });

            MSTestAssertions.That(() => sink.Events.Should().HaveCount(1));
            var traceEvent = sink.Events[0];

            MSTestAssertions.That(() => traceEvent.Kind.Should().Be("Email"));
            MSTestAssertions.That(() => traceEvent.MemberPath.Should().Be("Name"));
            MSTestAssertions.That(() => traceEvent.Decision.Should().Be(ValidationRuleDecision.Failed));
            MSTestAssertions.That(() => traceEvent.MemberPath.Contains("SECRET", StringComparison.Ordinal).Should().BeFalse());
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public void DisabledTrace_DoesNotAllocateOnTheValidPath() {
        using var sink = new DisabledTraceSink();
        ValidationDiagnostics.Sink = sink;
        var validator = new TraceFluentValidator();
        var model = new TraceModel { Name = "Valid", Age = 25 };

        try {
            for(var i = 0; i < 1_000; i++) {
                _ = validator.Validate(model);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();

            for(var i = 0; i < 1_000; i++) {
                _ = validator.Validate(model);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
#if DEBUG
            const int maximumAllocated = 32 * 1_000;
#else
            const int maximumAllocated = 0;
#endif
            MSTestAssertions.That(() => (allocated <= maximumAllocated).Should().BeTrue(
                $"the disabled trace path should not allocate, but allocated {allocated} bytes"));
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    [TestMethod]
    public void GeneratedAndFluent_ProduceIdenticalEventSequences() {
        const string source = """
            using System;
            using Jeninnet.Validation;
            using Jeninnet.Validation.Attributes;
            using Jeninnet.Validation.Attributes.Common;

            namespace TraceParity;

            [Validate("TraceParity")]
            public sealed class TraceParityModel {
                [NotEmpty] public string? Name { get; set; }
                [InclusiveBetween(18, 99)] public int Age { get; set; }
            }

            public static class TraceHarness {
                public static void Run() {
                    _ = new TraceParityModelValidator().Validate(
                        new TraceParityModel { Name = string.Empty, Age = 10 });
                }
            }
            """;

        var cancellationToken = TestCancellation;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                                  .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>()
                                  .ToArray();
        var compilation = CSharpCompilation.Create(
            $"RuleDecisionTraceAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
        var driver = CSharpGeneratorDriver.Create(new ValidatorGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, cancellationToken);
        var runResult = driver.GetRunResult();

        MSTestAssertions.That(() => (runResult.GeneratedTrees.Length > 0).Should().BeTrue(
            string.Join(Environment.NewLine, runResult.Diagnostics)));

        var generatedCompilation = compilation.AddSyntaxTrees(runResult.GeneratedTrees);

        using var stream = new MemoryStream();
        var emit = generatedCompilation.Emit(stream, cancellationToken: cancellationToken);

        MSTestAssertions.That(() => emit.Success.Should().BeTrue(
            string.Join(Environment.NewLine, emit.Diagnostics)));

        var assembly = Assembly.Load(stream.ToArray());
        var harness = assembly.GetType("TraceParity.TraceHarness", throwOnError: true)!;
        var sink = new TraceCaptureSink();
        ValidationDiagnostics.Sink = sink;

        try {
            _ = harness.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
            var generated = Snapshot(sink);
            sink.Reset();

            _ = new TraceFluentValidator().Validate(new TraceModel { Name = string.Empty, Age = 10 });
            var fluent = Snapshot(sink);

            MSTestAssertions.That(() => generated.Should().Be(fluent));
            MSTestAssertions.That(() => generated.Should().Be(
                "NotEmpty@Name:Failed | InclusiveBetween@Age:Failed"));
        }
        finally {
            ValidationDiagnostics.Sink = null;
        }
    }

    private static string Snapshot(TraceCaptureSink sink) =>
        string.Join(
            " | ",
            sink.Events.Select(static traceEvent =>
                $"{traceEvent.Kind}@{traceEvent.MemberPath}:{traceEvent.Decision}"));

    private static CompositeValidator<TraceModel> CreateGeneratedShapeValidator() {
        ISyncValidationRule<TraceModel>[] rules = [
            new NotEmptyRule<TraceModel, string?>(
                ValidatorFactory.Accessor<TraceModel, string?>(static x => x.Name, nameof(TraceModel.Name)),
                message: null),
            new InclusiveBetweenRule<TraceModel, int>(
                ValidatorFactory.Accessor<TraceModel, int>(static x => x.Age, nameof(TraceModel.Age)),
                18,
                99,
                message: null)
        ];

        return new CompositeValidator<TraceModel>(rules);
    }

    private CancellationToken TestCancellation => TestContext?.CancellationToken ?? CancellationToken.None;

    public TestContext? TestContext { get; set; }
}
