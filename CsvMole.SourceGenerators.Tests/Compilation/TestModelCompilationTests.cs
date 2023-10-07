namespace CsvMole.SourceGenerators.Tests.Compilation;

public class TestModelCompilationTests
{
    [Test]
    public void TestCompilationModel_CompilesAndParses()
    {
        // Arrange
        const string text = "Id\n1";
        using var stringReader = new StringReader(text);

        // Act
        var result = CompilationParser.Parse(stringReader)
            .ToList();

        // Assert
        CollectionAssert.AllItemsAreNotNull(result);
        CollectionAssert.IsNotEmpty(result);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.That(first.Id, Is.EqualTo("1"));
    }
}