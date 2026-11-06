using System.Collections.Immutable;
using CsvMole.SourceGenerators.External;
using CsvMole.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CsvMole.SourceGenerators.Analysis;

internal static class ParserAnalyzer
{
    internal static readonly SymbolDisplayFormat TypeFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    // Renders "ReturnType Name<T>(params) where T : ..." without defaults; modifiers come from the declaration syntax.
    private static readonly SymbolDisplayFormat SignatureFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
            | SymbolDisplayGenericsOptions.IncludeTypeConstraints
            | SymbolDisplayGenericsOptions.IncludeVariance,
        memberOptions: SymbolDisplayMemberOptions.IncludeType
            | SymbolDisplayMemberOptions.IncludeParameters
            | SymbolDisplayMemberOptions.IncludeRef,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType
            | SymbolDisplayParameterOptions.IncludeName
            | SymbolDisplayParameterOptions.IncludeParamsRefOut
            | SymbolDisplayParameterOptions.IncludeExtensionThis,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
            | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static ParserModel Analyze(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var syntax = (TypeDeclarationSyntax)context.TargetNode;
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        var methods = ImmutableArray.CreateBuilder<MethodModel>();
        // One analysis (and one set of model diagnostics) per distinct model type in the class.
        var models = new Dictionary<ITypeSymbol, ModelInfo?>(SymbolEqualityComparer.Default);

        var containing = new List<ContainingType>();
        for (var parent = syntax.Parent as TypeDeclarationSyntax; parent is not null; parent = parent.Parent as TypeDeclarationSyntax)
        {
            containing.Insert(0, new ContainingType(Keyword(parent), parent.Identifier.Text, AccessModifiers(parent.Modifiers)));
        }

        var classError = ClassError(syntax);
        if (classError is not null)
        {
            diagnostics.Add(DiagnosticInfo.Create("CSVM001", syntax.Identifier.GetLocation(), type.Name, classError));
        }
        else
        {
            foreach (var member in type.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (member is IMethodSymbol { IsPartialDefinition: true, PartialImplementationPart: null } method
                    && method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) is MethodDeclarationSyntax declaration
                    && RequiresImplementation(method, declaration))
                {
                    methods.Add(AnalyzeMethod(method, declaration, context.SemanticModel.Compilation, diagnostics, models));
                }
            }
        }

        var ns = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();
        var hintParts = new List<string>();
        if (ns is not null) hintParts.Add(ns);
        for (var t = type.ContainingType; t is not null; t = t.ContainingType) hintParts.Insert(ns is null ? 0 : 1, t.Name);
        hintParts.Add(type.Name);

        return new ParserModel(
            ns,
            containing.ToImmutableArray().AsEquatableArray(),
            AccessModifiers(syntax.Modifiers),
            syntax.Identifier.Text,
            string.Join(".", hintParts) + ".g.cs",
            methods.ToImmutable().AsEquatableArray(),
            diagnostics.ToImmutable().AsEquatableArray());
    }

    private static string? ClassError(TypeDeclarationSyntax syntax)
    {
        if (syntax is not ClassDeclarationSyntax) return "must be a partial class";
        if (!syntax.Modifiers.Any(SyntaxKind.PartialKeyword)) return "must be declared partial";
        if (syntax.TypeParameterList is not null) return "must not be generic";
        if (syntax.Modifiers.Any(SyntaxKind.FileKeyword)) return "must not be file-local";
        for (var parent = syntax.Parent as TypeDeclarationSyntax; parent is not null; parent = parent.Parent as TypeDeclarationSyntax)
        {
            if (!parent.Modifiers.Any(SyntaxKind.PartialKeyword)) return $"must be nested only in partial types ('{parent.Identifier.Text}' is not partial)";
            if (parent.Modifiers.Any(SyntaxKind.FileKeyword)) return $"must not be nested in a file-local type ('{parent.Identifier.Text}')";
            if (parent.TypeParameterList is not null) return $"must not be nested in a generic type ('{parent.Identifier.Text}')";
        }
        return null;
    }

    private static MethodModel AnalyzeMethod(
        IMethodSymbol method, MethodDeclarationSyntax declaration, Compilation compilation,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics, Dictionary<ITypeSymbol, ModelInfo?> models)
    {
        string? error = null;
        var isAsync = false;
        var modelType = "";
        ITypeSymbol? modelSymbol = null;

        if (method.ReturnType is INamedTypeSymbol { TypeArguments.Length: 1 } returnType
            && (IsType(returnType.OriginalDefinition, compilation, "System.Collections.Generic.IEnumerable`1")
                || (isAsync = IsType(returnType.OriginalDefinition, compilation, "System.Collections.Generic.IAsyncEnumerable`1"))))
        {
            modelSymbol = returnType.TypeArguments[0];
            modelType = modelSymbol.ToDisplayString(TypeFormat);
        }
        else
        {
            error = "the return type must be IEnumerable<T> or IAsyncEnumerable<T>";
        }

        if (method.IsGenericMethod) error ??= "parser methods cannot be generic";

        var parameters = method.Parameters;
        if (parameters.Length == 0 || parameters[0].RefKind != RefKind.None
            || !IsTextReader(parameters[0].Type, compilation))
        {
            error ??= "the first parameter must be a System.IO.TextReader or a type derived from it";
        }

        string? optionsName = null, cancellationName = null;
        foreach (var parameter in parameters.Skip(1))
        {
            if (parameter.RefKind == RefKind.None && optionsName is null && IsType(parameter.Type, compilation, "CsvMole.CsvOptions"))
            {
                optionsName = parameter.Name;
            }
            else if (parameter.RefKind == RefKind.None && cancellationName is null && IsType(parameter.Type, compilation, "System.Threading.CancellationToken"))
            {
                cancellationName = parameter.Name;
                if (!isAsync) error ??= "a CancellationToken parameter is only supported on async (IAsyncEnumerable<T>) methods";
            }
            else
            {
                error ??= $"parameter '{parameter.Name}' is not supported (allowed: TextReader, CsvOptions?, CancellationToken)";
            }
        }

        var location = declaration.Identifier.GetLocation();
        if (error is not null)
        {
            diagnostics.Add(DiagnosticInfo.Create("CSVM002", location, method.Name, error));
        }
        else if (isAsync && cancellationName is null)
        {
            diagnostics.Add(DiagnosticInfo.Create("CSVM009", location, method.Name));
        }

        ModelInfo? model = null;
        if (error is null && modelSymbol is not null && !models.TryGetValue(modelSymbol, out model))
        {
            var modelDiagnostics = new List<DiagnosticInfo>();
            models[modelSymbol] = model = ModelAnalyzer.Analyze(modelSymbol, compilation, modelDiagnostics);
            diagnostics.AddRange(modelDiagnostics);
        }

        return new MethodModel(
            method.Name,
            AccessModifiers(declaration.Modifiers),
            isAsync,
            modelType,
            parameters.Length > 0 ? parameters[0].Name : "",
            optionsName,
            cancellationName,
            method.ReturnType.ToDisplayString(TypeFormat),
            model,
            string.Join(" ", declaration.Modifiers.Select(m => m.Text)) + " " + method.ToDisplayString(SignatureFormat));
    }

    // Ruling M: a partial method the compiler lets go unimplemented (void, no accessibility/virtual/override/
    // sealed/new/extern modifier, no out params) is an optional hook, not a parser method.
    private static bool RequiresImplementation(IMethodSymbol method, MethodDeclarationSyntax declaration) =>
        !method.ReturnsVoid
        || method.Parameters.Any(p => p.RefKind == RefKind.Out)
        || declaration.Modifiers.Any(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.InternalKeyword
            or SyntaxKind.ProtectedKeyword or SyntaxKind.PrivateKeyword or SyntaxKind.VirtualKeyword
            or SyntaxKind.OverrideKeyword or SyntaxKind.SealedKeyword or SyntaxKind.NewKeyword or SyntaxKind.ExternKeyword);

    private static bool IsType(ITypeSymbol type, Compilation compilation, string metadataName) =>
        SymbolEqualityComparer.Default.Equals(type, compilation.GetTypeByMetadataName(metadataName));

    private static bool IsTextReader(ITypeSymbol type, Compilation compilation)
    {
        var textReader = compilation.GetTypeByMetadataName("System.IO.TextReader");
        for (var t = type; t is not null; t = t.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(t, textReader)) return true;
        }
        return false;
    }

    private static string AccessModifiers(SyntaxTokenList modifiers) =>
        string.Join(" ", modifiers
            .Where(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.InternalKeyword
                or SyntaxKind.ProtectedKeyword or SyntaxKind.PrivateKeyword)
            .Select(m => m.Text));

    private static string Keyword(TypeDeclarationSyntax type) => type switch
    {
        RecordDeclarationSyntax r when r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "record struct",
        RecordDeclarationSyntax => "record",
        _ => type.Keyword.Text,
    };
}
