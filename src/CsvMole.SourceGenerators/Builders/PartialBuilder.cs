using System.CodeDom.Compiler;
using System.Collections.Immutable;
using CsvMole.SourceGenerators.Models;

namespace CsvMole.SourceGenerators.Builders;

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

        indentedWriter.WriteLine($"public partial class {partialDeclaration.ClassName}");
        indentedWriter.WriteLine("{");
        indentedWriter.Indent++;

        // Initialise the converters
        var converters = partialDeclaration.Methods
            .SelectMany(x => x.Properties)
            .Select(x => x.Converter)
            .Where(x => x is not null)
            .ToImmutableArray();

        InitializeConverters(indentedWriter, converters);

        // Create method signature
        foreach ( var methodDeclaration in partialDeclaration.Methods )
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

    private static void InitializeConverters(IndentedTextWriter indentedTextWriter,
        ImmutableArray<ConverterDeclaration?> converters)
    {
        foreach (var converter in Enumerable.OfType<ConverterDeclaration>(converters))
        {
            indentedTextWriter.WriteLine($"private readonly {converter.Type} {converter.GetStaticReadonlyVariableName()} = new {converter.Type}();");
        }
    }
}