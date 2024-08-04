using System.Text;
using CsvMole.SourceGenerators.Builders;
using CsvMole.SourceGenerators.Extensions;
using CsvMole.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CsvMole.SourceGenerators;

[Generator]
internal sealed class CsvParserIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            /* the attribute you want to filter by */
            "CsvMole.Abstractions.Attributes.CsvParserAttribute",
            /* filter by nodes you want */
            static (_, _) => true,
            /* turn the attribute and node into a model containing what you need to generate source code */
            GetSemanticTargetForGeneration);

        context.RegisterSourceOutput(classDeclarations, static (spc, source) => Execute(source, spc));
    }

    private static PartialDeclaration GetSemanticTargetForGeneration(
        GeneratorAttributeSyntaxContext context,
        CancellationToken token)
    {
        if ( context.TargetSymbol is not INamedTypeSymbol namedTypeSymbol )
            throw new Exception("TargetSymbol is not INamedTypeSymbol");
        
        return namedTypeSymbol.GetPartialDeclaration();
    }

    private static void Execute(PartialDeclaration partialDeclaration, SourceProductionContext context)
    {
        var builder = new PartialBuilder(partialDeclaration);
        var result = builder.Build();

        context.AddSource($"{partialDeclaration.ClassName}.g.cs", SourceText.From(result, Encoding.UTF8));
    }
}