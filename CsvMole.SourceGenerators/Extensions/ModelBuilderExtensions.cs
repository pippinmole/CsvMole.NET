using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CsvMole.Source.Extensions;

internal static class ModelBuilderExtensions
{
    public static ImmutableArray<MethodDeclaration> GetMethodDeclarations(this INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(static x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(static x => x.IsStatic)
            .Select(ToModel)
            .ToImmutableArray();
    }

    public static PartialDeclaration GetPartialDeclaration(this INamedTypeSymbol classSymbol)
    {
        var methodModels = classSymbol.GetMethodDeclarations();

        return new PartialDeclaration(
            classSymbol.ContainingNamespace.ToDisplayString(),
            classSymbol.Name,
            methodModels
        );
    }

    private static DelimiterAttributeDeclaration? GetDelimiter(ISymbol classModelSymbol)
    {
        var attribute = GetAttributeFrom(classModelSymbol, "CsvMole.Abstractions.Attributes.CsvDelimiterAttribute");
        if ( attribute is null )
            return null;

        var delimiterCharacter = attribute.ConstructorArguments[0].Value?.ToString();
        return delimiterCharacter is null ? null : new DelimiterAttributeDeclaration(delimiterCharacter);
    }
    
    private static MethodDeclaration ToModel(IMethodSymbol methodSymbol)
    {
        // This should be StringReader. TODO: Check this
        var parameters = methodSymbol.Parameters
            .Select(x => new ParameterDeclaration(x.Name, x.Type.ToString()))
            .ToImmutableArray();

        // Outer = IEnumerable<T>, Inner = T
        var (outer, inner) = ExtractIEnumerableGenericType(methodSymbol);
        
        _ = inner ?? throw new Exception("Inner is null");
        _ = outer ?? throw new Exception("Outer is null");

        // Get the properties from the model which the Csv Parser will return
        var properties = GetPropertiesFromClass(inner);
        var delimiter = GetDelimiter(inner);
        
        return new MethodDeclaration(
            methodSymbol.Name,
            outer.ToString(),
            inner.ToString(),
            delimiter,
            parameters,
            properties
        );
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

    private static AttributeData? GetAttributeFrom(ISymbol symbol, string fullName)
    {
        var attributes = symbol.GetAttributes();

        foreach ( var attribute in attributes )
        {
            // TODO: use typeof(CsvConverterAttribute).FullName here
            if ( attribute.AttributeClass?.ToDisplayString() == fullName )
                return attribute;
        }

        return null;
    }

    private static ConverterDeclaration? GetConverterAttributeFrom(ISymbol propertySymbol)
    {
        var attribute = GetAttributeFrom(propertySymbol, "CsvMole.Abstractions.Attributes.CsvConverterAttribute");
        if ( attribute is null )
            return null;
        
        var converterType = attribute.ConstructorArguments[0].Value?.ToString();
        return converterType is null ? null : new ConverterDeclaration(converterType);
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
    
    public static ImmutableArray<IPropertySymbol>? ExtractPropertiesFromType(ITypeSymbol? modelSymbol)
    {
        return modelSymbol?.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.IsStatic == false)
            .ToImmutableArray();
    }
}