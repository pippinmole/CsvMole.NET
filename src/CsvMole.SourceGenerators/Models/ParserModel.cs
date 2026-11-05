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

/// <param name="Delimiter">From [CsvDelimiter] on the model type, if present.</param>
internal sealed record ModelInfo(string TypeFullName, char? Delimiter, EquatableArray<MemberInfo> Members, ConstructorInfo Constructor);

/// <param name="Order">The [CsvOrder] index, or -1 when absent.</param>
/// <param name="UnderlyingTypeFullName">The type to parse: T for Nullable&lt;T&gt;, the enum, or the ISpanParsable type.</param>
/// <param name="IsNullable">True for Nullable&lt;T&gt; members, and for converter members of a Nullable&lt;T&gt; or annotated
/// reference type (built-in string nullability is in <see cref="ParseKind"/>). A missing column then gives null.</param>
/// <param name="CtorParameterIndex">Index in the chosen constructor, or -1 for initializer-assigned members.</param>
/// <param name="IsRequired">A C# <c>required</c> member: it must also be set in the object initializer unless
/// <see cref="ConstructorInfo.SetsRequiredMembers"/>.</param>
internal sealed record MemberInfo(
    string Name,
    string TypeFullName,
    string? ColumnName,
    int Order,
    ParseKind Kind,
    string? UnderlyingTypeFullName,
    bool IsNullable,
    string? ConverterTypeFullName,
    MemberAssignment Assignment,
    int CtorParameterIndex,
    bool IsRequired);

// Nullable<T> is represented by MemberInfo.IsNullable on the inner kind.
internal enum ParseKind { String, NullableString, SpanParsable, Enum, Converter }

internal enum MemberAssignment { Constructor, Initializer }

/// <param name="ParameterTypeFullNames">In order; empty means the parameterless constructor.</param>
/// <param name="ParameterNames">Unescaped names, parallel to the types. Unmapped optional parameters are left out of
/// the call, so the emitter passes arguments by name.</param>
/// <param name="SetsRequiredMembers">The constructor has [SetsRequiredMembers].</param>
internal sealed record ConstructorInfo(
    EquatableArray<string> ParameterTypeFullNames, EquatableArray<string> ParameterNames, bool SetsRequiredMembers);
