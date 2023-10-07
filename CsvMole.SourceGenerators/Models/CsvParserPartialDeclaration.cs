using CsvMole.Source.External;

namespace CsvMole.Source;

internal record CsvParserPartialDeclaration(
    string Namespace,
    string ClassName,
    EquatableArray<CsvParserMethodDeclaration> Methods
);

internal record CsvParserMethodDeclaration(
    string MethodName,
    string ParameterType,
    string ReturnType,
    string InnerReturnType,
    EquatableArray<CsvParserProperty> Properties
);

internal record CsvParserProperty(
    string Name,
    string Type
);