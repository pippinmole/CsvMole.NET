using System.Collections.Immutable;
using System.Text;
using CsvMole.Source.Builders;
using Microsoft.CodeAnalysis;
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

    private static PartialDeclaration GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context,
        CancellationToken token)
    {
        var classSymbol = context.TargetSymbol as INamedTypeSymbol ??
                          throw new Exception("Target symbol is null or not a class");
        var methodModels = GetMethodDeclarationsFrom(classSymbol);

        return new PartialDeclaration(
            classSymbol.ContainingNamespace.ToDisplayString(),
            classSymbol.Name,
            methodModels
        );
    }

    private static MethodDeclaration ToModel(IMethodSymbol methodSymbol)
    {
        // This should be StringReader. TODO: Check this
        var parameterType = methodSymbol.Parameters[0].Type;

        // Outer = IEnumerable<T>, Inner = T
        var (outer, inner) = ExtractIEnumerableGenericType(methodSymbol);
        
        _ = inner ?? throw new Exception("Inner is null");
        _ = outer ?? throw new Exception("Outer is null");

        // Get the properties from the model which the Csv Parser will return
        var properties = GetPropertiesFromClass(inner);
        
        return new MethodDeclaration(
            methodSymbol.Name,
            parameterType.ToString(),
            outer.ToString(),
            inner.ToString(),
            properties
        );
    }

    /// <summary>
    /// Given an <see cref="ITypeSymbol"/>, return an <see cref="ImmutableArray{T}"/> of <see cref="PropertyDeclaration"/>
    /// </summary>
    /// <param name="classSymbol"></param>
    /// <returns></returns>
    private static ImmutableArray<PropertyDeclaration> GetPropertiesFromClass(ITypeSymbol classSymbol)
    {
        var properties = ExtractPropertiesFromType(classSymbol);
        
        if ( properties is null )
            return ImmutableArray<PropertyDeclaration>.Empty;
        
        var result = new List<PropertyDeclaration>();
        
        foreach ( var property in properties )
        {
            var converter = GetConverterAttributeFrom(property);
            var asModel = new PropertyDeclaration(
                property.Name,
                property.Type.ToString(),
                converter
            );

            result.Add(asModel);
        }

        return result.ToImmutableArray();
    }
    
    private static ImmutableArray<MethodDeclaration> GetMethodDeclarationsFrom(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.IsStatic)
            .Select(ToModel)
            .ToImmutableArray();
    }

    public static (ITypeSymbol? IEnumerableType, ITypeSymbol? InnerType) ExtractIEnumerableGenericType(
        IMethodSymbol method)
    {
        var returnType = method.ReturnType;

        if ( returnType is not INamedTypeSymbol { IsGenericType: true } namedTypeSymbol ||
             namedTypeSymbol.OriginalDefinition.ToDisplayString() != "System.Collections.Generic.IEnumerable<T>" )
            return (null, null);
        
        // Now, genericTypeSymbol represents the 'T' in 'IEnumerable<T>'
        var genericTypeSymbol = namedTypeSymbol.TypeArguments[0];
        return (returnType, genericTypeSymbol);
    }

    public static ImmutableArray<IPropertySymbol>? ExtractPropertiesFromType(ITypeSymbol? modelSymbol)
    {
        return modelSymbol?.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.IsStatic == false)
            .ToImmutableArray();
    }

    private static ConverterDeclaration? GetConverterAttributeFrom(IPropertySymbol propertySymbol)
    {
        var attributes = propertySymbol.GetAttributes();
        
        foreach ( var attribute in attributes )
        {
            var attributeType = attribute.AttributeClass?.ToDisplayString();

            if ( attributeType == "CsvMole.Abstractions.Attributes.CsvConverter" )
            {
                var converterType = attribute.ConstructorArguments[0].Value?.ToString();

                if ( converterType is null )
                    continue;

                return new ConverterDeclaration(converterType);
            }
        }

        return null;
    }

    public static void Execute(PartialDeclaration partialDeclaration, SourceProductionContext context)
    {
        var builder = new PartialBuilder(partialDeclaration);
        var result = builder.Build();
        
        context.AddSource($"{partialDeclaration.ClassName}.g.cs", SourceText.From(result, Encoding.UTF8));
    }
}