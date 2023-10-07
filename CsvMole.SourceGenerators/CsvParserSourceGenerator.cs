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
    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            /* the attribute you want to filter by */
            "CsvParserAttribute",
            /* filter by nodes you want */
            static (_, _) => true,
            /* turn the attribute and node into a model containing what you need to generate source code */
            GetSemanticTargetForGeneration);

        context.RegisterSourceOutput(classDeclarations, static (spc, source) => Execute(source, spc));
    }

    private static CsvParserModel GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context,
        CancellationToken token)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.TargetNode;
        var classDeclaration = (ClassDeclarationSyntax)methodDeclaration.Parent!;

        var methodName = methodDeclaration.Identifier.ValueText;
        var className = classDeclaration.Identifier.ValueText;

        var namespaceName = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name
            .ToString() ?? throw new NullReferenceException("Please define a namespace for your class");

        var parameterType = methodDeclaration.ParameterList.Parameters[0].Type!.ToString();
        var returnType = methodDeclaration.ReturnType.ToString();
        var innerReturnType = returnType.Replace("System.Collections.Generic.IEnumerable<", "").Replace(">", "");

        var properties = methodDeclaration.ParameterList.Parameters[0].Type!.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Select(x => new CsvParserProperty(x.Identifier.ValueText, x.Identifier.ValueText)).ToImmutableArray()
            .AsEquatableArray();

        return new CsvParserModel(namespaceName, className, methodName, parameterType, returnType, innerReturnType,
            properties);
    }

    public static void Execute(CsvParserModel model, SourceProductionContext context)
    {
        var builder = new CsvParserBuilder(model);
        var result = builder.Build();

        context.AddSource($"{model.ClassName}.g.cs", SourceText.From(result, Encoding.UTF8));
    }
}