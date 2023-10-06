using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CsvMole.Source.Builders
{
    internal sealed class CsvParserBuilder
    {
        private readonly string? _namespaceName;
        private readonly SemanticModel _semanticModel;
        private readonly BaseTypeDeclarationSyntax _class;

        public CsvParserBuilder(SemanticModel semanticModel, BaseTypeDeclarationSyntax @class)
        {
            _namespaceName = (@class.Parent as NamespaceDeclarationSyntax)?.Name.ToString()
                             ?? (@class.Parent as FileScopedNamespaceDeclarationSyntax)?.Name.ToString();
            _semanticModel = semanticModel;
            _class = @class;
        }

        public string Build()
        {
            var sb = new StringBuilder();
        
            sb.AppendLine("using CsvMole.Example.Models;");
            sb.AppendLine("using System.Linq;");

            if ( !string.IsNullOrEmpty(_namespaceName) )
            {
                sb.AppendLine($"namespace {_namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"     public partial class {_class.Identifier.Text}");
            sb.AppendLine("      {");
            
            var methods = _class.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach ( var method in methods )
            {
                // Create method signature
                var builder = new MethodBuilder(_semanticModel, method);
                sb.Append(builder.Build());
            }

            sb.AppendLine("  }");

            if ( !string.IsNullOrEmpty(_namespaceName) )
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }
    }
}