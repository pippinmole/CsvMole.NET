using Microsoft.CodeAnalysis;

namespace CsvMole.Source;

[Generator]
internal sealed class CsvParserSourceGenerator : ISourceGenerator
{
    private const string CsvParserName = "CsvParser";

    public void Initialize(GeneratorInitializationContext context)
        => context.RegisterForSyntaxNotifications(() => new CsvParserReceiver());

    public void Execute(GeneratorExecutionContext context)
    {
        if ( context.SyntaxContextReceiver is CsvParserReceiver receiver )
        {
            var results = CsvParserGenerator.GenerateMappings(receiver, context.Compilation, context.AnalyzerConfigOptions);

            foreach ( var (diagnostics, name, text) in results )
            {
                foreach ( var diagnostic in diagnostics )
                {
                    context.ReportDiagnostic(diagnostic);
                }

                if ( name is not null && text is not null )
                {
                    context.DiagnosticLog("CG001", "Source Generator", $"Creating class {name} with text {text}");
                    context.AddSource(name, text);
                }
            }
        }
            
        // context.DiagnosticLog("CG000", "Source Generator", "Source generator started");
        //
        // // Get semantic model
        // var semanticModel = context.Compilation.GetSemanticModel(context.Compilation.SyntaxTrees.First());
        //
        // var classesWithAttribute = context.Compilation.SyntaxTrees
        //     .SelectMany(st => st
        //         .GetRoot()
        //         .DescendantNodes()
        //         .OfType<ClassDeclarationSyntax>()
        //         .Where(r => r.AttributeLists
        //                         .SelectMany(al => al.Attributes)
        //                         .Any(a =>
        //                             a.Name.ToString().EndsWith(CsvParserName) ||
        //                             a.Name.ToString().EndsWith(CsvParserName+"Attribute")
        //                         )
        //                     && r.Modifiers.Any(m =>
        //                         m.IsKind(SyntaxKind.PartialKeyword)))); // Ensure it's a partial class
        //
        // foreach ( var classWithAttribute in classesWithAttribute )
        // {
        //     var sourceBuilder = new StringBuilder();
        //     
        //     var classBuilder = new CsvParserBuilder(semanticModel, classWithAttribute);
        //     sourceBuilder.Append(classBuilder.Build());
        //     
        //     // Log all sourcebuilder text
        //     // context.DiagnosticLog("CG001", "Source Generator", sourceBuilder.ToString().ReplaceLineEndings(string.Empty));
        //
        //     // foreach ( var line in sourceBuilder.ToString().Split("\n") )
        //     //     context.DiagnosticLog("CG001", "Source Generator", line);
        //     
        //     // Log that we're creating this class
        //     context.DiagnosticLog("CG001", "Source Generator", $"Creating class {classWithAttribute.Identifier.Text}.g.cs");
        //     
        //     context.AddSource($"{classWithAttribute.Identifier.Text}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        // }
        //     
        // context.DiagnosticLog("CG001", "Source Generator", "Source generator finished");
    }
}

internal static class Extensions
{
    public static void DiagnosticLog(this GeneratorExecutionContext context, string id, string title, string message)
    {
        var diagnosticDescriptor = new DiagnosticDescriptor(id, title, message, "Generator", DiagnosticSeverity.Warning, true);
        context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, Location.None));
    }
}