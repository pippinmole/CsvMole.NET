using System.CodeDom.Compiler;
using System.Text;

namespace CsvMole.Source.Builders;

internal sealed class MethodBuilder(IndentedTextWriter indentedTextWriter,
    CsvParserMethodDeclaration partialDeclaration)
{
    private const string ParserMethodName = "Parse";

    public void Build()
    {
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine($"public static partial {partialDeclaration.ReturnType} {ParserMethodName}({partialDeclaration.ParameterType} stringReader)");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;

        // Iterate each line of the StringBuilder
        indentedTextWriter.WriteLine("while (stringReader.ReadLine() is { } line)");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;
        
        indentedTextWriter.WriteLine($"var model = new {partialDeclaration.InnerReturnType}();");
        
        // Split line into values
        indentedTextWriter.WriteLine("var values = line.Split(',');");
        
        var i = 0;
        foreach ( var property in partialDeclaration.Properties )
        {
            indentedTextWriter.WriteLine($"model.{property.Name} = values[{i}];");
            i++;
        }
        
        indentedTextWriter.WriteLine("yield return model;");
        
        indentedTextWriter.Indent--;
        indentedTextWriter.WriteLine("}");
        indentedTextWriter.Indent--;
        
        // Close while loop
        indentedTextWriter.WriteLine("}");    
    }
}