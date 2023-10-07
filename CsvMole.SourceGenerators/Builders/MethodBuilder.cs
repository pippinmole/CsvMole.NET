using System.CodeDom.Compiler;
using System.Text;

namespace CsvMole.Source.Builders;

internal sealed class MethodBuilder(IndentedTextWriter indentedTextWriter,
    CsvParserPartialDeclaration partialDeclaration)
{
    private const string ParserMethodName = "Parse";

    public void Build()
    {
        foreach ( var method in partialDeclaration.Methods )
        {
            BuildPartial(method);
        }
    }

    private void BuildPartial(CsvParserMethodDeclaration methodDeclaration)
    {
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine($"public partial {methodDeclaration.ReturnType} {ParserMethodName}({methodDeclaration.ParameterType} stringReader)");
        indentedTextWriter.WriteLine("{");
        indentedTextWriter.Indent++;

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