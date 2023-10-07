using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Options;
using CsvMole.SourceGenerators.Tests.Compilation.Models;

namespace CsvMole.SourceGenerators.Tests.Compilation;

[CsvParser]
public static partial class CompilationParser
{
    public static partial IEnumerable<TestCompilationModel> Parse(StringReader stringReader, CsvOptions? options = null);
}