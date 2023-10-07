using System.CodeDom.Compiler;

namespace CsvMole.Source.Builders;

internal sealed class PartialBuilder(PartialDeclaration partialDeclaration)
{
    public string Build()
    {
        using var writer = new StringWriter();
        using var indentedWriter = new IndentedTextWriter(writer, "  ");

        indentedWriter.WriteLine("using System.Linq;");
        indentedWriter.WriteLine();
        
        if ( !string.IsNullOrEmpty(partialDeclaration.Namespace) )
        {
            indentedWriter.WriteLine($"namespace {partialDeclaration.Namespace}");
            indentedWriter.WriteLine("{");
            indentedWriter.Indent++; // Increase the indentation
        }

        indentedWriter.WriteLine($"public static partial class {partialDeclaration.ClassName}");
        indentedWriter.WriteLine("{");
        indentedWriter.Indent++;

        // Create method signature
        foreach(var methodDeclaration in partialDeclaration.Methods)
        {
            var builder = new MethodBuilder(indentedWriter, methodDeclaration);
            builder.Build();
        }

        indentedWriter.Indent--; // Decrease the indentation
        indentedWriter.WriteLine("}");

        if ( !string.IsNullOrEmpty(partialDeclaration.Namespace) )
        {
            indentedWriter.Indent--; // Decrease the indentation
            indentedWriter.WriteLine("}");
        }

        return writer.ToString();
    }
}