using CsvMole.SourceGenerators.Analysis;
using CsvMole.SourceGenerators.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CsvMole.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class CsvParserGenerator : IIncrementalGenerator
{
    /// <summary>Tracking name of the analysed-model step (used by caching tests).</summary>
    public const string ParserModelStep = "ParserModel";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var parsers = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "CsvMole.CsvParserAttribute",
                static (node, _) => node is ClassDeclarationSyntax,
                ParserAnalyzer.Analyze)
            .WithTrackingName(ParserModelStep);

        context.RegisterSourceOutput(parsers, static (output, parser) =>
        {
            foreach (var diagnostic in parser.Diagnostics)
            {
                output.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            // No methods means an invalid class (CSVM001) or nothing to implement.
            if (!parser.Methods.IsEmpty)
            {
                output.AddSource(parser.HintName, ParserEmitter.Emit(parser));
            }
        });
    }
}
