using CsvMole.Abstractions.Attributes;

namespace CsvMole.SourceGenerators.Tests.Compilation.Models;

public class TestCompilationModel
{
    [CsvOrder(0)]
    public string Id { get; set; } = null!;
}