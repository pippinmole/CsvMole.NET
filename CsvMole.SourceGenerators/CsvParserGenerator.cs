using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using CsvMole.Source.Builders;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CsvMole.Source;

[Generator]
internal sealed class CsvParserGenerator
{
    public static ImmutableArray<(ImmutableArray<Diagnostic> diagnostics, string? name, SourceText? text)>
        GenerateMappings(CsvParserReceiver receiver, Compilation compilation,
            AnalyzerConfigOptionsProvider optionsProvider)
    {
        using var writer = new StringWriter();
        using var indentedWriter = new IndentedTextWriter(writer, "    ");
        
        var results = ImmutableArray.CreateBuilder<(ImmutableArray<Diagnostic> diagnostic, string? name, SourceText? text)>();
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());

        foreach ( var target in receiver.Targets )
        {
            // Get partial classes
            var classes = target.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .Where(cds => cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

            foreach ( var @class in classes )
            {
                // Generate class signature
                var builder = new CsvParserBuilder(semanticModel, @class);
                var sourceText = SourceText.From(builder.Build(), Encoding.UTF8);
                
                // Add source
                results.Add((ImmutableArray<Diagnostic>.Empty, $"{target.Name}_CsvParser.cs", sourceText));
            }
        }

        return results.ToImmutable();
    }
}

internal sealed class CsvParserInformation
{
    private readonly Compilation _compilation;
    private readonly SyntaxNode _currentNode;
    private readonly List<INamedTypeSymbol> _source;

    public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

    public CsvParserInformation(CsvParserReceiver receiver, SyntaxNode currentNode, Compilation compilation)
    {
        _currentNode = currentNode;
        _compilation = compilation;
        _source = receiver.Targets;

        Validate();
    }

    private void Validate()
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        
        // If the source is not a partial class, report an error
        if ( _source.Any(x => x.DeclaringSyntaxReferences.Any(r =>
                r.GetSyntax() is not ClassDeclarationSyntax cds ||
                !cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))) )
        {
            diagnostics.Add(Diagnostic.Create(
                new DiagnosticDescriptor("CG001", "CsvParser", "The class '{0}' is not a partial class", "CsvParser",
                    DiagnosticSeverity.Error, true), _currentNode.GetLocation(), _source));
        }

        Diagnostics = diagnostics.ToImmutable();
    }
}