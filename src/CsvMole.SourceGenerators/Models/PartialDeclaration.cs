using CsvMole.SourceGenerators.External;

namespace CsvMole.SourceGenerators.Models;

internal sealed record PartialDeclaration(
    string Namespace,
    string ClassName,
    EquatableArray<MethodDeclaration> Methods
);

internal sealed record MethodDeclaration(
    string MethodName,
    string OuterReturnType,
    string InnerReturnType,
    DelimiterAttributeDeclaration? DelimiterAttribute,
    EquatableArray<ParameterDeclaration> Parameters,
    EquatableArray<PropertyDeclaration> Properties
);

internal sealed record ParameterDeclaration(
    string Name,
    string Type
);

internal sealed record PropertyDeclaration(
    string Name,
    string Type,
    ConverterDeclaration? Converter
);

/// <summary>
/// 
/// </summary>
internal sealed record ConverterDeclaration
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Type">The type of the converter. For example: typeof(DateTimeConverter).Name</param>
    public ConverterDeclaration(string Type)
    {
        this.Type = Type;
    }

    /// <summary>The type of the converter. For example: typeof(DateTimeConverter).Name</summary>
    public string Type { get; init; }

    public void Deconstruct(out string Type)
    {
        Type = this.Type;
    }

    public string GetStaticReadonlyVariableName()
    {
        // Turn CsvMole.Abstractions.Converters.CsvDateTimeConverter into CsvDateTimeConverter

        var lastIndexOf = Type.LastIndexOf('.');
        var substring = Type[(lastIndexOf + 1)..];
        return substring;
    }
}

internal sealed record DelimiterAttributeDeclaration(
    string Delimiter
);