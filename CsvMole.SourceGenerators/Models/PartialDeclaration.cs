using CsvMole.Source.External;

namespace CsvMole.Source;

internal record PartialDeclaration(
    string Namespace,
    string ClassName,
    EquatableArray<MethodDeclaration> Methods
);

internal record MethodDeclaration(
    string MethodName,
    string OuterReturnType,
    string InnerReturnType,
    DelimiterAttributeDeclaration? DelimiterAttribute,
    EquatableArray<ParameterDeclaration> Parameters,
    EquatableArray<PropertyDeclaration> Properties
);

internal record ParameterDeclaration(
    string Name,
    string Type
);

internal record PropertyDeclaration(
    string Name,
    string Type,
    ConverterDeclaration? Converter
);

/// <summary>
/// 
/// </summary>
/// <param name="Type">The type of the converter. For example: typeof(DateTimeConverter).Name</param>
internal record ConverterDeclaration(
    string Type
);

internal record DelimiterAttributeDeclaration(
    string Delimiter
);