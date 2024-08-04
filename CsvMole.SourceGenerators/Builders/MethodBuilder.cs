using System.CodeDom.Compiler;
using System.Collections.Immutable;
using CsvMole.SourceGenerators.Models;

namespace CsvMole.SourceGenerators.Builders;

internal sealed class MethodBuilder(IndentedTextWriter indentedTextWriter, MethodDeclaration methodDeclaration)
{
    public void Build()
    {
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine(
            $"public partial {methodDeclaration.OuterReturnType} {methodDeclaration.MethodName}({BuildParameters(methodDeclaration.Parameters)})");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;

        BuildSkipHeaderFromOptions(indentedTextWriter);
        
        // Iterate each line of the StringBuilder
        indentedTextWriter.WriteLine("while (stringReader.ReadLine() is { } line)");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;

        indentedTextWriter.WriteLine($"var model = new {methodDeclaration.InnerReturnType}();");

        // Split line into values
        var delimiter = methodDeclaration.DelimiterAttribute?.Delimiter ?? ",";
        indentedTextWriter.WriteLine($"var values = line.Split(\"{delimiter}\");");

        var i = 0;
        foreach ( var property in methodDeclaration.Properties )
        {
            indentedTextWriter.WriteLine(BuildProperty(property, i));
            i++;
        }

        indentedTextWriter.WriteLine("yield return model;");

        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");
        indentedTextWriter.Indent--;

        // Close while loop
        indentedTextWriter.WriteLine("}");
    }

    private static void BuildSkipHeaderFromOptions(IndentedTextWriter indentedTextWriter)
    {
        indentedTextWriter.WriteLine("if ( options?.HasHeader ?? false )");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;
        
        indentedTextWriter.WriteLine("stringReader.ReadLine();");
        
        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");
    }

    private static string BuildParameters(ImmutableArray<ParameterDeclaration> parameterDeclarations)
    {
        var parameters = parameterDeclarations.Select(x => $"{x.Type} {x.Name}");
        return string.Join(", ", parameters);
    }

    private static string BuildProperty(PropertyDeclaration propertyDeclaration, int index)
    {
        var converter = propertyDeclaration.Converter;
        return converter is null
            ? $"model.{propertyDeclaration.Name} = values[{index}];"
            : $"model.{propertyDeclaration.Name} = {converter.GetStaticReadonlyVariableName()}.ConvertFromString(values[{index}]);";
    }
}