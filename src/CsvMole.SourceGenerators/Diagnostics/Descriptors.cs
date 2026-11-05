using Microsoft.CodeAnalysis;

namespace CsvMole.SourceGenerators.Diagnostics;

internal static class Descriptors
{
    private const string Category = "CsvMole";

    private static readonly Dictionary<string, DiagnosticDescriptor> All = new[]
    {
        Error("CSVM001", "Invalid parser class", "Parser class '{0}' {1}"),
        Error("CSVM002", "Unsupported parser method signature", "Parser method '{0}' has an unsupported signature: {1}"),
        Error("CSVM003", "Member type cannot be parsed", "Member '{0}' of type '{1}' has no built-in parse and no converter"),
        Error("CSVM004", "Duplicate column mapping", "Model '{0}' has a duplicate {1} '{2}'"),
        Error("CSVM005", "Mapped member cannot be assigned", "Mapped member '{0}' cannot be assigned"),
        Error("CSVM006", "Required member is not mapped", "Required member '{0}' is not mapped to a column"),
        Error("CSVM007", "Ambiguous constructor", "Model '{0}' has several public constructors and none is marked [CsvConstructor]"),
        Error("CSVM008", "Invalid converter type", "Converter type '{0}' is invalid: {1}"),
        Warning("CSVM009", "Async parser method has no CancellationToken", "Async parser method '{0}' has no CancellationToken parameter"),
        Warning("CSVM010", "Model has no mapped members", "Model '{0}' has no mapped members"),
    }.ToDictionary(d => d.Id);

    public static DiagnosticDescriptor Get(string id) => All[id];

    private static DiagnosticDescriptor Error(string id, string title, string message) =>
        new(id, title, message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static DiagnosticDescriptor Warning(string id, string title, string message) =>
        new(id, title, message, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
}
