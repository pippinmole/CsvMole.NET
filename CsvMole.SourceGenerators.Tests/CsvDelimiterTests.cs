using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Options;

namespace CsvMole.SourceGenerators.Tests;

[CsvParser]
public static partial class CsvParser
{
    public static partial IEnumerable<CsvPipeDelimiterModel> ParsePipeDelimited(StringReader stringReader, CsvOptions? options);
    public static partial IEnumerable<CsvCommaDelimiterModel> ParseCommaDelimited(StringReader stringReader, CsvOptions? options);
    public static partial IEnumerable<NoDelimiterModel> ParseNoDelimiter(StringReader stringReader, CsvOptions? options);
}

public class NoDelimiterModel
{
    public string Id { get; set; } = null!;
    public string SecondValue { get; set; } = null!;
}

[CsvDelimiter("|")]
public class CsvPipeDelimiterModel
{
    public string Id { get; set; } = null!;
    public string SecondValue { get; set; } = null!;
}

[CsvDelimiter(",")]
public class CsvCommaDelimiterModel
{
    public string Id { get; set; } = null!;
    public string SecondValue { get; set; } = null!;
}

public class CsvDelimiterTests
{
    [Test]
    public void NoCsvDelimiterModel_DefaultsToComma_And_ParsesProperly_WithHeader()
    {
        // Arrange
        const string text = "Id,SecondValue\n1,2";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions { HasHeader = true };

        // Act
        var result = CsvParser.ParseNoDelimiter(stringReader, options).ToList();

        // Assert
        CollectionAssert.AllItemsAreNotNull(result);
        CollectionAssert.IsNotEmpty(result);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo("1"));
            Assert.That(first.SecondValue, Is.EqualTo("2"));
        });
    }
    
    [Test]
    public void NoCsvDelimiterModel_DefaultsToComma_And_ParsesProperly_WithNoHeader()
    {
        // Arrange
        const string text = "1,2";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions { HasHeader = false };

        // Act
        var result = CsvParser.ParseNoDelimiter(stringReader, options).ToList();

        // Assert
        CollectionAssert.AllItemsAreNotNull(result);
        CollectionAssert.IsNotEmpty(result);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo("1"));
            Assert.That(first.SecondValue, Is.EqualTo("2"));
        });
    }
    
    [Test]
    public void CsvPipeDelimiterModel_ParsesProperly()
    {
        // Arrange
        const string text = "Id|SecondValue\n1|2";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions { HasHeader = true };

        // Act
        var result = CsvParser.ParsePipeDelimited(stringReader, options).ToList();

        // Assert
        CollectionAssert.AllItemsAreNotNull(result);
        CollectionAssert.IsNotEmpty(result);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo("1"));
            Assert.That(first.SecondValue, Is.EqualTo("2"));
        });
    }
    
    [Test]
    public void CsvCommaDelimiterModel_ParsesProperly()
    {
        // Arrange
        const string text = "Id,SecondValue\n1,2";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions { HasHeader = true };

        // Act
        var result = CsvParser.ParseCommaDelimited(stringReader, options).ToList();

        // Assert
        CollectionAssert.AllItemsAreNotNull(result);
        CollectionAssert.IsNotEmpty(result);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo("1"));
            Assert.That(first.SecondValue, Is.EqualTo("2"));
        });
    }
    
    [Test]
    public void CsvCommaDelimiterModel_ParsesProperly_WithNoHeader()
    {
        // Arrange
        const string text = "1,2";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions { HasHeader = false };

        // Act
        var result = CsvParser.ParseCommaDelimited(stringReader, options).ToList();

        // Assert
        CollectionAssert.AllItemsAreNotNull(result);
        CollectionAssert.IsNotEmpty(result);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo("1"));
            Assert.That(first.SecondValue, Is.EqualTo("2"));
        });
    }
    
    [Test]
    public void CsvPipeDelimiterModel_ParsesProperly_WithNoHeader()
    {
        // Arrange
        const string text = "1|2";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions { HasHeader = false };

        // Act
        var result = CsvParser.ParsePipeDelimited(stringReader, options).ToList();

        // Assert
        CollectionAssert.AllItemsAreNotNull(result);
        CollectionAssert.IsNotEmpty(result);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo("1"));
            Assert.That(first.SecondValue, Is.EqualTo("2"));
        });
    }
}