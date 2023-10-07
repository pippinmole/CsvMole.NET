using System.CodeDom.Compiler;
using System.Collections.Immutable;

namespace CsvMole.Source.Builders;

internal sealed class MethodBuilder(IndentedTextWriter indentedTextWriter, MethodDeclaration methodDeclaration)
{
    public void Build()
    {
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine($"public static partial {methodDeclaration.ReturnType} {methodDeclaration.MethodName}({methodDeclaration.ParameterType} stringReader)");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;

        // Initialise the converters
        var converters = methodDeclaration.Properties
            .Select(x => x.Converter)
            .ToImmutableArray();
        
        for ( var index = 0; index < converters.Length; index++ )
        {
            var converter = converters[index];
            InitializeConverter(converter, index);
        }

        // Iterate each line of the StringBuilder
        indentedTextWriter.WriteLine("while (stringReader.ReadLine() is { } line)");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;
        
        indentedTextWriter.WriteLine($"var model = new {methodDeclaration.InnerReturnType}();");
        
        // Split line into values
        indentedTextWriter.WriteLine("var values = line.Split(',');");
        
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

    private void InitializeConverter(ConverterDeclaration? converter, int index)
    {
        if ( converter is null )
            return;

        indentedTextWriter.WriteLine($"var converter{index} = new {converter.Type}();");
    }

    private static string BuildProperty(PropertyDeclaration propertyDeclaration, int index)
    {
        var converter = propertyDeclaration.Converter;
        return converter is null
            ? $"model.{propertyDeclaration.Name} = values[{index}];"
            : $"model.{propertyDeclaration.Name} = converter{index}.ConvertFromString(values[{index}]);";
    }
}