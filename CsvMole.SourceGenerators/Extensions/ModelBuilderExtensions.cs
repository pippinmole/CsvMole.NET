using System.Collections.Immutable;
using CsvMole.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace CsvMole.SourceGenerators.Extensions;

internal static class ModelBuilderExtensions
{
    private static ImmutableArray<MethodDeclaration> GetMethodDeclarations(this INamedTypeSymbol classSymbol)
    {
        return [
            ..classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(static x => x.DeclaredAccessibility == Accessibility.Public && !x.ReturnsVoid)
                .Select(ToModel)
        ];
    }

    public static PartialDeclaration GetPartialDeclaration(this INamedTypeSymbol classSymbol)
    {
        return new PartialDeclaration(
            classSymbol.ContainingNamespace.ToDisplayString(),
            classSymbol.Name,
            classSymbol.GetMethodDeclarations()
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
        
        _ = inner ?? throw new NullReferenceException($"Inner is null for method: {methodSymbol.Name}");
        _ = outer ?? throw new NullReferenceException($"Outer is null for method: {methodSymbol.Name}");

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

    private static (ITypeSymbol? IEnumerableType, ITypeSymbol? InnerType) ExtractIEnumerableGenericType(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        
        if (returnType is not INamedTypeSymbol {IsGenericType: true} namedTypeSymbol)
            throw new NotSupportedException($"Return type of a CSV parser function has to be IEnumerable<T>: " + returnType.Name + " on method " + method);
        
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

        return [..result];
    }

    private static ImmutableArray<IPropertySymbol>? ExtractPropertiesFromType(ITypeSymbol? modelSymbol)
    {
        return modelSymbol?.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.IsStatic == false)
            .ToImmutableArray();
    }
}