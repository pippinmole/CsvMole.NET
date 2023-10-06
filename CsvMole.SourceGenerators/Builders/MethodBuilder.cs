using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CsvMole.Source.Builders;

internal sealed class MethodBuilder
{
    private const string ParserMethodName = "Parse";
    
    private readonly MethodDeclarationSyntax _method;
    private readonly TypeSyntax _returnType;
    private readonly TypeSyntax _innerReturnType;
    private readonly List<IPropertySymbol> _properties;
    private readonly TypeSyntax _parameterType;

    public MethodBuilder(SemanticModel semanticModel, MethodDeclarationSyntax method)
    { 
        var parameters = method.ParameterList.Parameters;
        if ( parameters.Count != 1 )
            throw new ArgumentException("Method must have exactly one parameter");
        
        _method = method ?? throw new ArgumentNullException(nameof(method));
        _parameterType = parameters[0].Type ?? throw new ArgumentException("Parameter must have a type");
        _returnType = method.ReturnType ?? throw new ArgumentException("Method must have a return type");
        _innerReturnType = _returnType switch
        {
            GenericNameSyntax genericNameSyntax => genericNameSyntax.TypeArgumentList.Arguments[0],
            _ => throw new ArgumentException("Return type must be IEnumerable<T>")
        };
        
        var returnTypeSymbol = semanticModel.GetTypeInfo(_innerReturnType).Type;
        if ( returnTypeSymbol is INamedTypeSymbol namedTypeSymbol )
            _properties = namedTypeSymbol.GetMembers().OfType<IPropertySymbol>().ToList();
        else
            throw new ArgumentException("Return type must be IEnumerable<T>");

        if ( _parameterType.ToString() != "StringReader" ) throw new ArgumentException("Parameter must be StringReader");
        if ( !_returnType.ToString().Contains("IEnumerable") ) throw new ArgumentException("Return type must be IEnumerable<T>");
        
        // if (_returnType is GenericNameSyntax { Identifier.Text: "IEnumerable" } genericType)
        // {
        //     var innerTypeSyntax = genericType.TypeArgumentList.Arguments[0];
        //
        //     // Get the symbol for the inner type
        //     var innerTypeSymbol = semanticModel.GetTypeInfo(innerTypeSyntax).Type;
        //
        //     // Retrieve all properties of that type
        //     _properties = innerTypeSymbol!.GetMembers().OfType<IPropertySymbol>().ToList();
        // }
    }

    public string Build()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"         public partial {_returnType} {ParserMethodName}({_parameterType} stringReader)");
        sb.AppendLine("          {");

        // Create method body
        // Get all properties and their [CsvOrder] attributes

        sb.AppendLine($"// Got {_properties.Count} properties for return type222: {_innerReturnType}");
        
        // Iterate each line of the StringBuilder
        sb.AppendLine("while (stringReader.ReadLine() is { } line)");
        sb.AppendLine("     {");
        
        sb.AppendLine("var model = new CustomModel();");
        
        // Split line into values
        sb.AppendLine("var values = line.Split(',');");
        
        // sb.AppendLine("System.Console.WriteLine($\"Got {values.Length} values\");");
        
        for ( var i = 0; i < _properties.Count; i++ )
        {
            var property = _properties[i];
            
            sb.AppendLine($"model.{property.Name} = values[{i}];");
        }
        
        sb.AppendLine("yield return model;");
        
        sb.AppendLine("     }");
        
        // Close while loop
        sb.AppendLine("      }");
        

        return sb.ToString();
    }
}