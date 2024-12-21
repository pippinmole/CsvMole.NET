using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Options;

namespace CsvMole.SourceGenerators.Tests;

[CsvParser]
public partial class CsvParser
{
    public partial IEnumerable<CsvPipeDelimiterModel> ParsePipeDelimited(StringReader stringReader, CsvOptions? options);

    public partial IEnumerable<CsvCommaDelimiterModel> ParseCommaDelimited(StringReader stringReader, CsvOptions? options);

    public partial IEnumerable<NoDelimiterModel> ParseNoDelimiter(StringReader stringReader, CsvOptions? options);
}

public sealed class NoDelimiterModel
{
    public string Id { get; set; } = null!;
    public string SecondValue { get; set; } = null!;
}

[CsvDelimiter("|")]
public sealed class CsvPipeDelimiterModel
{
    public string Id { get; set; } = null!;
    public string SecondValue { get; set; } = null!;
}

[CsvDelimiter(",")]
public sealed class CsvCommaDelimiterModel
{
    public string Id { get; set; } = null!;
    public string SecondValue { get; set; } = null!;
}

public sealed class CsvDelimiterTests
{
    [Test]
    public void NoCsvDelimiterModel_DefaultsToComma_And_ParsesProperly_WithHeader()
    {
        // Arrange
        const string text = "Id,SecondValue\n1,2";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions { HasHeader = true };
        var parser = new CsvParser();

        // Act
        var result = parser.ParseNoDelimiter(stringReader, options).ToList();

        // Assert
        Assert.That(result, Is.All.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo("1"));
            Assert.That(first.SecondValue, Is.EqualTo("2"));
        });
    }
    
    // [Test]
    // public void NoCsvDelimiterModel_WithEscapedCommaCell_DoesntSplitOnEscaped_WithoutHeader()
    // {
    //     // Arrange
    //     const string text = "1,\"2,3\""; // 1,"2,3"
    //     using var stringReader = new StringReader(text);
    //
    //     var options = new CsvOptions { HasHeader = false };
    //     var parser = new CsvParser();
    //
    //     // Act
    //     var result = parser.ParseNoDelimiter(stringReader, options).ToList();
    //
    //     // Assert
    //     Assert.That(result, Is.All.Not.Null);
    //     Assert.That(result, Is.Not.Empty);
    //     Assert.That(result, Has.Count.EqualTo(1));
    //
    //     var first = result[0];
    //     Assert.Multiple(() =>
    //     {
    //         Assert.That(first.Id, Is.EqualTo("1"));
    //         Assert.That(first.SecondValue, Is.EqualTo("2,3"));
    //     });
    // }
    
    [Test]
    public void NoCsvDelimiterModel_DefaultsToComma_And_ParsesProperly_WithNoHeader()
    {
        // Arrange
        const string text = "1,2";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions { HasHeader = false };
        var parser = new CsvParser();

        // Act
        var result = parser.ParseNoDelimiter(stringReader, options).ToList();

        // Assert
        Assert.That(result, Is.All.Not.Null);
        Assert.That(result, Is.Not.Empty);
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
        var parser = new CsvParser();

        // Act
        var result = parser.ParsePipeDelimited(stringReader, options).ToList();

        // Assert
        Assert.That(result, Is.All.Not.Null);
        Assert.That(result, Is.Not.Empty);
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
        var parser = new CsvParser();
        
        // Act
        var result = parser.ParseCommaDelimited(stringReader, options).ToList();

        // Assert
        Assert.That(result, Is.All.Not.Null);
        Assert.That(result, Is.Not.Empty);
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
        var parser = new CsvParser();

        // Act
        var result = parser.ParseCommaDelimited(stringReader, options).ToList();

        // Assert
        Assert.That(result, Is.All.Not.Null);
        Assert.That(result, Is.Not.Empty);
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
        var parser = new CsvParser();

        // Act
        var result = parser.ParsePipeDelimited(stringReader, options).ToList();

        // Assert
        Assert.That(result, Is.All.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo("1"));
            Assert.That(first.SecondValue, Is.EqualTo("2"));
        });
    }
}