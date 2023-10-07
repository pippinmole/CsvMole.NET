using CsvMole.Source.External;

namespace CsvMole.Source;

internal record PartialDeclaration(
    string Namespace,
    string ClassName,
    EquatableArray<MethodDeclaration> Methods
);

internal record MethodDeclaration(
    string MethodName,
    string ParameterType,
    string ReturnType,
    string InnerReturnType,
    EquatableArray<PropertyDeclaration> Properties
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