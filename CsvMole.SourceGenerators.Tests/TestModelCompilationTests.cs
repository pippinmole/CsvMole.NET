using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Options;

namespace CsvMole.SourceGenerators.Tests;

[CsvParser]
public partial class CompilationParser
{
    public partial IEnumerable<TestCompilationModel> Parse(StringReader stringReader, CsvOptions? options);
}

public class TestCompilationModel
{
    [CsvOrder(0)]
    public string Id { get; set; } = null!;
}

public class TestModelCompilationTests
{
    [Test]
    public void CompilationParser_OnlyOnePartial_HasHeader_CompilesAndParses()
    {
        // Arrange
        const string text = "Id\n1";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions
        {
            HasHeader = true
        };
        var parser = new CompilationParser();

        // Act
        var result = parser.Parse(stringReader, options)
            .ToList();

        // Assert
        Assert.That(result, Is.All.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.That(first.Id, Is.EqualTo("1"));
    }
    
    [Test]
    public void CompilationParser_OnlyOnePartial_NoHeader_CompilesAndParses()
    {
        // Arrange
        const string text = "1";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions
        {
            HasHeader = false
        };
        var parser = new CompilationParser();

        // Act
        var result = parser.Parse(stringReader, options)
            .ToList();

        // Assert
        Assert.That(result, Is.All.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.That(first.Id, Is.EqualTo("1"));
    }
}