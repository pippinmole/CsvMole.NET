using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Options;
using CsvMole.SourceGenerators.Tests.Compilation.Models;

namespace CsvMole.SourceGenerators.Tests.Compilation;

[CsvParser]
public static partial class CompilationParser
{
    public static partial IEnumerable<TestCompilationModel> Parse(StringReader stringReader, CsvOptions? options = null);
}

public class TestModelCompilationTests
{
    [Test]
    public void CompilationParser_OnlyOnePartial_CompilesAndParses()
    {
        // Arrange
        const string text = "Id\n1";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions
        {
            HasHeader = true
        };

        // Act
        var result = CompilationParser.Parse(stringReader, options)
            .ToList();

        // Assert
        CollectionAssert.AllItemsAreNotNull(result);
        CollectionAssert.IsNotEmpty(result);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.That(first.Id, Is.EqualTo("1"));
    }
}