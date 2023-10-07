using CsvMole.Source.External;

namespace CsvMole.Source;

internal record CsvParserModel(
    string Namespace,
    string ClassName,
    string MethodName,
    string ParameterType,
    string ReturnType,
    string InnerReturnType,
    EquatableArray<CsvParserProperty> Properties
);