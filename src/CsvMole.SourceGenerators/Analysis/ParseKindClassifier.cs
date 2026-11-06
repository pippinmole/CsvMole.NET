using CsvMole.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace CsvMole.SourceGenerators.Analysis;

internal static class ParseKindClassifier
{
    /// <summary>Picks the built-in parse for a member type, or null when there is none (a converter is needed).</summary>
    public static (ParseKind Kind, bool IsNullable, string? Underlying)? Classify(ITypeSymbol t, Compilation c)
    {
        if (t.SpecialType == SpecialType.System_String)
        {
            return (t.NullableAnnotation == NullableAnnotation.Annotated ? ParseKind.NullableString : ParseKind.String, false, null);
        }

        var isNullable = false;
        if (t is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            isNullable = true;
            t = nullable.TypeArguments[0];
        }

        if (t.TypeKind == TypeKind.Error) return null; // callers skip error types; the compiler reports them

        var underlying = t.ToDisplayString(ParserAnalyzer.TypeFormat);
        if (t.TypeKind == TypeKind.Enum) return (ParseKind.Enum, isNullable, underlying);

        // All the spec's built-in types (bool, char, integers, floats, decimal, dates, TimeSpan, Guid) implement this on .NET 10.
        // GetTypeByMetadataName returns null when several assemblies define the type; then take the core library's.
        var spanParsable = c.GetTypeByMetadataName("System.ISpanParsable`1")
            ?? c.GetTypesByMetadataName("System.ISpanParsable`1").FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(
                i.ContainingAssembly, c.GetSpecialType(SpecialType.System_Object).ContainingAssembly));
        if (spanParsable is not null && t.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, spanParsable)
                && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], t)))
        {
            return (ParseKind.SpanParsable, isNullable, underlying);
        }

        return null;
    }
}
