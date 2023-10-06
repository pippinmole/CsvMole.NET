using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CsvMole.Source;

internal sealed class CsvParserReceiver : ISyntaxContextReceiver
{
    public List<INamedTypeSymbol> Targets { get; } = new();
    
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        var syntaxNode = context.Node;
        var model = context.SemanticModel;

        if ( syntaxNode is TypeDeclarationSyntax )
        {
            var csvParserAttributeSymbol = model.Compilation.GetTypeByMetadataName("CsvMole.Abstractions.Attributes.CsvParserAttribute");

            if ( model.GetDeclaredSymbol(syntaxNode) is INamedTypeSymbol typeSymbol )
            {
                foreach ( var typeAttribute in typeSymbol.GetAttributes() )
                {
                    if ( SymbolEqualityComparer.Default.Equals(typeAttribute.AttributeClass!,
                            csvParserAttributeSymbol) )
                    {
                        Targets.Add(typeSymbol);
                    }
                }
            }
        }
    }
}