using CsvMole.SourceGenerators.External;

namespace CsvMole.SourceGenerators.Models;

// Pipeline models: equatable values only (no ISymbol / SyntaxNode / Location) so incremental caching works.

internal sealed record ParserModel(
    string? Namespace,
    EquatableArray<ContainingType> ContainingTypes,
    string Accessibility,
    string ClassName,
    string HintName,
    EquatableArray<MethodModel> Methods,
    EquatableArray<DiagnosticInfo> Diagnostics);

/// <param name="Keyword">"class", "struct", "record", "record struct" or "interface".</param>
internal sealed record ContainingType(string Keyword, string Name, string Accessibility);

/// <param name="Model">Null when the method is invalid; the emitter then writes a throwing stub.</param>
/// <param name="Signature">The full implementation signature (modifiers, return type, name, type parameters,
/// parameters, constraints) rendered to match the user's partial declaration.</param>
internal sealed record MethodModel(
    string Name,
    string Accessibility,
    bool IsAsync,
    string ModelTypeFullName,
    string ReaderParamName,
    string? OptionsParamName,
    string? CancellationParamName,
    string ReturnTypeFullName,
    ModelInfo? Model,
    string Signature);

// Placeholder (controller ruling F); Task 8 replaces it with the analysed model shape.
internal sealed record ModelInfo;
