using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CsvMole.SourceGenerators.Tests;

internal static class GeneratorHarness
{
    public static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    private static readonly ImmutableArray<MetadataReference> References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)),
        MetadataReference.CreateFromFile(typeof(CsvParserAttribute).Assembly.Location),
    ];

    public static CSharpCompilation CreateCompilation(string source) =>
        CSharpCompilation.Create(
            "Tests",
            [CSharpSyntaxTree.ParseText(source, ParseOptions, path: "Source.cs")],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    public static GeneratorDriver CreateDriver() =>
        CSharpGeneratorDriver.Create(
            [new CsvParserGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

    public static (ImmutableArray<Diagnostic> GeneratorDiagnostics, ImmutableArray<Diagnostic> CompilationDiagnostics, GeneratorDriver Driver) Run(string source)
    {
        var driver = CreateDriver().RunGeneratorsAndUpdateCompilation(CreateCompilation(source), out var output, out var generatorDiagnostics);
        var compilationDiagnostics = output.GetDiagnostics()
            .Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .ToImmutableArray();
        return (generatorDiagnostics, compilationDiagnostics, driver);
    }
}
