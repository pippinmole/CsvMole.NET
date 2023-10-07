using System.Collections.Immutable;
using System.Text;
using CsvMole.Source.Builders;
using CsvMole.Source.External;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CsvMole.Source;

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

    private static CsvParserPartialDeclaration GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context,
        CancellationToken token)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
        
        var className = classDeclaration.Identifier.ValueText;
        var namespaceName = classDeclaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().First().Name.ToString();
        
        var methodDeclarations = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().ToImmutableArray();
        var methodModels = new List<CsvParserMethodDeclaration>();
        
        foreach ( var method in methodDeclarations )
        {
            var methodName = method.Identifier.ValueText;
            var parameterType = method.ParameterList.Parameters[0].Type!.ToString();
            var returnType = method.ReturnType.ToString();
            var innerReturnType = returnType.Replace("IEnumerable<", "").Replace(">", "");

            var properties = method.ParameterList.Parameters[0].Type!.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(x => new CsvParserProperty(x.Identifier.ValueText, x.Identifier.ValueText)).ToImmutableArray()
                .AsEquatableArray(); 
            
            methodModels.Add(new CsvParserMethodDeclaration(methodName, parameterType, returnType, innerReturnType, properties));
        }

        return new CsvParserPartialDeclaration(namespaceName, className, methodModels.ToImmutableArray());
    }

    public static void Execute(CsvParserPartialDeclaration partialDeclaration, SourceProductionContext context)
    {
        var builder = new PartialBuilder(partialDeclaration);
        var result = builder.Build();

        context.AddSource($"{partialDeclaration.ClassName}.g.cs", SourceText.From(result, Encoding.UTF8));
    }
}