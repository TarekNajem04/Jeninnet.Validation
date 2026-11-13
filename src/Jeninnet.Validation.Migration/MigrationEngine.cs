//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Migration;

/// <summary>
/// One migration observation for a source file.
/// </summary>
/// <param name="Kind">The machine-readable kind.</param>
/// <param name="Line">The 1-based line number.</param>
/// <param name="Detail">A human-readable description.</param>
internal sealed record MigrationFinding(string Kind, int Line, string Detail) {
    /// <summary>RuleFor gained a member-name argument.</summary>
    public const string RuleForMemberName = "rulefor-member-name";

    /// <summary>A WithErrorCode call was renamed to WithCode.</summary>
    public const string WithCodeRenamed = "withcode-renamed";

    /// <summary>A Required attribute was converted to NotNull with a review note.</summary>
    public const string RequiredConverted = "required-converted";

    /// <summary>A validator class was converted to a fluent validator.</summary>
    public const string ValidatorConverted = "validator-converted";

    /// <summary>A pattern needs human review and was left untouched.</summary>
    public const string NeedsReview = "needs-review";
}

/// <summary>
/// The migration outcome for one source file.
/// </summary>
internal sealed class FileMigration {
    /// <summary>Gets the display path of the file.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the rewritten source text.</summary>
    public required string NewText { get; set; }

    /// <summary>Gets a value indicating whether the text changed.</summary>
    public required bool Changed { get; set; }

    /// <summary>Gets the applied conversions in file order.</summary>
    public List<MigrationFinding> Converted { get; } = [];

    /// <summary>Gets the items left for humans in file order.</summary>
    public List<MigrationFinding> NeedsReview { get; } = [];

    internal void AddConverted(MigrationFinding finding) => Converted.Add(finding);

    internal void AddNeedsReview(MigrationFinding finding) => NeedsReview.Add(finding);
}

/// <summary>
/// Migrates one C# source text from FluentValidation / DataAnnotations idioms to Jeninnet
/// idioms using syntax analysis only (no project references required). Only known-safe patterns
/// are rewritten; everything else is reported for human review.
/// </summary>
internal static class MigrationEngine {
    /// <summary>
    /// Analyzes and rewrites one source text.
    /// </summary>
    /// <param name="path">The display path used in findings.</param>
    /// <param name="text">The source text.</param>
    /// <returns>The migration outcome for the file.</returns>
    public static FileMigration MigrateSource(string path, string text) {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(text);

        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetCompilationUnitRoot();

        var migration = new FileMigration {
            Path = path,
            NewText = text,
            Changed = false
        };

        var rewriter = new MigrationRewriter(migration);
        var rewritten = (CompilationUnitSyntax)rewriter.Visit(root);
        var newText = rewritten.ToFullString();

        migration.NewText = newText;
        migration.Changed = !string.Equals(newText, text, StringComparison.Ordinal);
        return migration;
    }

    private sealed class MigrationRewriter(FileMigration migration) : CSharpSyntaxRewriter {
        private static int GetLine(SyntaxNode node) =>
            node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        private static bool IsAbstractValidatorBase(BaseTypeSyntax baseType, out string modelType) {
            modelType = string.Empty;
            var type = baseType.Type;

            while(type is QualifiedNameSyntax qualified) {
                type = qualified.Right;
            }

            if(type is not GenericNameSyntax generic ||
               !string.Equals(generic.Identifier.Text, "AbstractValidator", StringComparison.Ordinal) ||
               generic.TypeArgumentList.Arguments.Count != 1) {
                return false;
            }

            modelType = generic.TypeArgumentList.Arguments[0].ToString();
            return true;
        }

        private static bool TryGetMemberPath(ExpressionSyntax expression, string parameterName, out string path) {
            path = string.Empty;
            var segments = new Stack<string>();
            var current = expression;

            while(current is MemberAccessExpressionSyntax access) {
                segments.Push(access.Name.Identifier.Text);
                current = access.Expression;
            }

            if(current is not IdentifierNameSyntax identifier ||
               !string.Equals(identifier.Identifier.Text, parameterName, StringComparison.Ordinal)) {
                return false;
            }

            path = string.Join(".", segments);
            return segments.Count > 0;
        }

        private static bool IsAbstractValidatorName(TypeSyntax type) {
            while(type is QualifiedNameSyntax qualified) {
                type = qualified.Right;
            }

            return type is GenericNameSyntax generic &&
                string.Equals(generic.Identifier.Text, "AbstractValidator", StringComparison.Ordinal);
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) {
            var validatorBase = node.BaseList?.Types.FirstOrDefault(static baseType =>
                baseType.Type is TypeSyntax type && IsAbstractValidatorName(type));

            if(validatorBase is null) {
                return base.VisitClassDeclaration(node);
            }

            if(!IsAbstractValidatorBase(validatorBase, out var modelType)) {
                return base.VisitClassDeclaration(node);
            }

            var constructors = node.Members.OfType<ConstructorDeclarationSyntax>().ToList();

            if(constructors.Count != 1 || constructors[0].ParameterList.Parameters.Count != 0) {
                migration.AddNeedsReview(new MigrationFinding(
                    MigrationFinding.NeedsReview,
                    GetLine(node),
                    $"Class '{node.Identifier.Text}' derives from FluentValidation AbstractValidator<{modelType}> but has no single parameterless constructor; Jeninnet Configure has a fixed builder signature, so convert it to Configure manually."));
                return base.VisitClassDeclaration(node);
            }

            var originalConstructor = constructors[0];
            var originalConstructorLine = GetLine(originalConstructor);

            // Record RuleFor findings from the ORIGINAL syntax. Nodes reached through ancestors
            // rebuilt below resolve their lines against a synthetic tree, so recording must happen
            // before any With* call on this path. The structural result is discarded here; the real
            // rewrite runs on the base-visited statements with recording disabled.
            RecordRuleForFindings(originalConstructor, modelType);

            var rewritten = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
            var baseList = rewritten.BaseList;
            var rewrittenBase = baseList?.Types.FirstOrDefault(static baseType =>
                baseType.Type is TypeSyntax type && IsAbstractValidatorName(type));

            if(baseList is null || rewrittenBase is null) {
                return rewritten;
            }

            var newBase = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"Validator<{modelType}>"));
            var bases = newBase.WithLeadingTrivia(rewrittenBase.GetLeadingTrivia())
                               .WithTrailingTrivia(rewrittenBase.GetTrailingTrivia());
            rewritten = rewritten.WithBaseList(baseList.WithTypes(
                baseList.Types.Replace(rewrittenBase, bases)));

            var constructor = rewritten.Members.OfType<ConstructorDeclarationSyntax>().Single();
            var statements = constructor.Body?.Statements ?? [];

            if(constructor.Initializer is not null) {
                migration.AddNeedsReview(new MigrationFinding(
                    MigrationFinding.NeedsReview,
                    originalConstructorLine,
                    $"Constructor of '{node.Identifier.Text}' has a base/this initializer; Jeninnet Configure takes only the rule builder, so migrate its arguments manually."));
            }

            var ctorLeading = constructor.GetLeadingTrivia().ToFullString();
            var braceEol = GetNewLine(constructor);

            var memberIndent = "    ";
            var lastBreak = ctorLeading.LastIndexOf('\n');
            if(lastBreak >= 0 && lastBreak + 1 < ctorLeading.Length) {
                memberIndent = ctorLeading[(lastBreak + 1)..];
            }

            if(memberIndent.Length == 0) {
                memberIndent = "    ";
            }

            var header = (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
                $"protected override void Configure(ValidatorRuleBuilder<{modelType}> builder)")!;
            var braceLeading = SyntaxFactory.TriviaList(
                SyntaxFactory.EndOfLine(braceEol),
                SyntaxFactory.Whitespace(memberIndent));
            var body = SyntaxFactory.Block(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                    .WithLeadingTrivia(braceLeading)
                    .WithTrailingTrivia(SyntaxFactory.EndOfLine(braceEol)),
                RewriteStatements(statements, modelType, recordFindings: false),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                    .WithLeadingTrivia(SyntaxFactory.Whitespace(memberIndent)));
            var configure = header
                .WithBody(body)
                .WithLeadingTrivia(constructor.GetLeadingTrivia())
                .WithTrailingTrivia(constructor.GetTrailingTrivia());

            rewritten = rewritten.WithMembers(rewritten.Members.Replace(constructor, configure));

            migration.AddConverted(new MigrationFinding(
                MigrationFinding.ValidatorConverted,
                GetLine(node),
                $"Class '{node.Identifier.Text}' converts from FluentValidation AbstractValidator<{modelType}> to Jeninnet Validator<{modelType}> with Configure: Jeninnet builds rules in the overridden Configure method instead of the constructor."));

            return rewritten;
        }

        private void RecordRuleForFindings(ConstructorDeclarationSyntax constructor, string modelType) {
            foreach(var statement in constructor.Body?.Statements ?? []) {
                RewriteStatement(statement, modelType, recordFindings: true);
            }
        }

        private SyntaxList<StatementSyntax> RewriteStatements(
            SyntaxList<StatementSyntax> statements, string modelType, bool recordFindings) {
            var rewritten = new List<StatementSyntax>(statements.Count);

            foreach(var statement in statements) {
                rewritten.Add(RewriteStatement(statement, modelType, recordFindings));
            }

            return SyntaxFactory.List(rewritten);
        }

        private StatementSyntax RewriteStatement(StatementSyntax statement, string modelType, bool recordFindings) {
            if(statement is not ExpressionStatementSyntax expressionStatement) {
                return statement;
            }

            return expressionStatement.WithExpression(RewriteExpression(expressionStatement.Expression, modelType, recordFindings));
        }

        private ExpressionSyntax RewriteExpression(ExpressionSyntax expression, string modelType, bool recordFindings) {
            if(expression is not InvocationExpressionSyntax invocation) {
                return expression;
            }

            // Descend through fluent chains (RuleFor(...).NotEmpty()...) to the RuleFor call.
            if(invocation.Expression is MemberAccessExpressionSyntax chained &&
               chained.Expression is InvocationExpressionSyntax receiver) {
                var rewrittenReceiver = RewriteExpression(receiver, modelType, recordFindings);

                return ReferenceEquals(rewrittenReceiver, receiver)
                    ? invocation
                    : invocation.WithExpression(chained.WithExpression(rewrittenReceiver));
            }

            var isRuleFor = (invocation.Expression switch {
                MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                _ => string.Empty
            }).Equals("RuleFor", StringComparison.Ordinal);

            if(!isRuleFor) {
                return expression;
            }

            var builderCall = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("builder"),
                SyntaxFactory.IdentifierName("RuleFor"))
                .WithLeadingTrivia(invocation.Expression.GetLeadingTrivia());

            if(invocation.ArgumentList.Arguments.Count != 1) {
                return invocation.WithExpression(builderCall);
            }

            if(invocation.ArgumentList.Arguments[0].Expression is not SimpleLambdaExpressionSyntax lambda ||
               lambda.Body is not ExpressionSyntax selector ||
               !TryGetMemberPath(selector, lambda.Parameter.Identifier.Text, out var path)) {
                if(recordFindings) {
                    migration.AddNeedsReview(new MigrationFinding(
                        MigrationFinding.NeedsReview,
                        GetLine(invocation),
                        "FluentValidation RuleFor uses a complex selector; Jeninnet needs an explicit member name for paths and localized messages — add the member-name argument manually."));
                }

                return invocation.WithExpression(builderCall);
            }

            var memberName = path.Contains('.', StringComparison.Ordinal)
                ? $"\"{path}\""
                : $"nameof({modelType}.{path})";

            if(recordFindings) {
                migration.AddConverted(new MigrationFinding(
                    MigrationFinding.RuleForMemberName,
                    GetLine(invocation),
                    $"FluentValidation RuleFor member '{path}' gained the explicit member name Jeninnet requires for member paths and localized messages."));
            }

            return invocation
                .WithExpression(builderCall)
                .WithArgumentList(invocation.ArgumentList.AddArguments(
                    SyntaxFactory.Argument(SyntaxFactory.ParseExpression(memberName))
                        .WithLeadingTrivia(SyntaxFactory.Space)));
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node) {
            if(node.Expression is MemberAccessExpressionSyntax member &&
               string.Equals(member.Name.Identifier.Text, "WithErrorCode", StringComparison.Ordinal)) {
                migration.AddConverted(new MigrationFinding(
                    MigrationFinding.WithCodeRenamed,
                    GetLine(node),
                    "FluentValidation WithErrorCode was renamed to Jeninnet WithCode: error codes travel on WithCode in Jeninnet."));
                return node.WithExpression(member.WithName(SyntaxFactory.IdentifierName("WithCode")));
            }

            if(node.Expression is IdentifierNameSyntax identifier &&
               string.Equals(identifier.Identifier.Text, "WithErrorCode", StringComparison.Ordinal)) {
                migration.AddConverted(new MigrationFinding(
                    MigrationFinding.WithCodeRenamed,
                    GetLine(node),
                    "FluentValidation WithErrorCode was renamed to Jeninnet WithCode: error codes travel on WithCode in Jeninnet."));
                return node.WithExpression(SyntaxFactory.IdentifierName("WithCode"));
            }

            if(node.Expression is MemberAccessExpressionSyntax unsupported &&
               (string.Equals(unsupported.Name.Identifier.Text, "Custom", StringComparison.Ordinal) ||
                string.Equals(unsupported.Name.Identifier.Text, "Transform", StringComparison.Ordinal))) {
                migration.AddNeedsReview(new MigrationFinding(
                    MigrationFinding.NeedsReview,
                    GetLine(node),
                    $"'{unsupported.Name.Identifier.Text}' is a FluentValidation escape hatch with no direct equivalent in Jeninnet: rewrite it against Jeninnet's error collector (value, context, errors) and remove this marker."));
            }

            return base.VisitInvocationExpression(node);
        }

        private static bool IsRequiredAttribute(AttributeSyntax node) {
            var name = node.Name switch {
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
                GenericNameSyntax generic => generic.Identifier.Text,
                _ => string.Empty
            };

            return string.Equals(name, "Required", StringComparison.Ordinal) ||
                string.Equals(name, "RequiredAttribute", StringComparison.Ordinal);
        }

        private static string GetNewLine(SyntaxNode node) {
            var endOfLine = node.GetFirstToken().GetPreviousToken().TrailingTrivia
                .LastOrDefault(static trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                .ToFullString();
            return endOfLine.Length == 0 ? "\n" : endOfLine;
        }

        public override SyntaxNode? VisitAttributeList(AttributeListSyntax node) {
            var hasRequired = node.Attributes.Any(static attribute => IsRequiredAttribute(attribute));
            var visited = (AttributeListSyntax)base.VisitAttributeList(node)!;

            if(!hasRequired) {
                return visited;
            }

            var indent = visited.GetLeadingTrivia();
            var comment = SyntaxFactory.Comment(
                "// TODO(Jeninnet): DataAnnotations [Required] became Jeninnet [NotNull]; use [NotEmpty] when empty strings must fail.");
            var leading = indent
                .Add(comment)
                .Add(SyntaxFactory.EndOfLine(GetNewLine(node)))
                .AddRange(indent);
            return visited.WithLeadingTrivia(leading);
        }

        public override SyntaxNode? VisitAttribute(AttributeSyntax node) {
            if(!IsRequiredAttribute(node)) {
                return base.VisitAttribute(node);
            }

            migration.AddConverted(new MigrationFinding(
                MigrationFinding.RequiredConverted,
                GetLine(node),
                "DataAnnotations [Required] became Jeninnet [NotNull] with a review note: [Required] also rejects empty strings, so switch to [NotEmpty] when empty strings must fail."));
            migration.AddNeedsReview(new MigrationFinding(
                MigrationFinding.NeedsReview,
                GetLine(node),
                "Review the DataAnnotations [Required] to Jeninnet [NotNull] mapping: use [NotEmpty] when empty strings must fail (for example when AllowEmptyStrings is false)."));

            return node.WithName(SyntaxFactory.ParseName("NotNull"));
        }
    }
}
