using System.CodeDom.Compiler;

namespace CsvMole.Source.Builders;

internal sealed class CsvParserBuilder(CsvParserModel model)
{
    public string Build()
    {
        using var writer = new StringWriter();
        using var indentedWriter = new IndentedTextWriter(writer, "\t");

        indentedWriter.WriteLine("using CsvMole.Example.Models;");
        indentedWriter.WriteLine("using System.Linq;");

        if ( !string.IsNullOrEmpty(model.Namespace) )
        {
            indentedWriter.WriteLine($"namespace {model.Namespace}");
            indentedWriter.WriteLine("{");
            indentedWriter.Indent++; // Increase the indentation
        }

        indentedWriter.WriteLine($"public partial class {model.ClassName}");
        indentedWriter.WriteLine("{");
        indentedWriter.Indent++;


        // Create method signature
        var builder = new MethodBuilder(model);
        indentedWriter.WriteLine(builder.Build());

        indentedWriter.Indent--; // Decrease the indentation
        indentedWriter.WriteLine("}");

        if ( !string.IsNullOrEmpty(model.Namespace) )
        {
            indentedWriter.Indent--; // Decrease the indentation
            indentedWriter.WriteLine("}");
        }

        return writer.ToString();
    }
}