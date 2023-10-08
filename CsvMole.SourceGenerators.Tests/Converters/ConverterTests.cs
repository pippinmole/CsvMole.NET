using System.ComponentModel;
using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Options;

namespace CsvMole.SourceGenerators.Tests.Converters;

[CsvParser]
public static partial class ConverterParser
{
    public static partial IEnumerable<ConverterModel> Parse(StringReader stringReader, CsvOptions? options);
}

public class ConverterModel
{
    public string Id { get; set; } = null!;
    
    [CsvConverter(typeof(DateTimeConverter))]
    public DateTime Date { get; set; }
}

public class ConverterTests
{
    [Test]
    public void DateTimeConverter_ConvertsProperly()
    {
        // Arrange
        const string text = "Id,Date\n1,2021-01-01";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions { HasHeader = true };

        // Act
        var result = ConverterParser.Parse(stringReader, options)
            .ToList();

        // Assert
        CollectionAssert.AllItemsAreNotNull(result);
        CollectionAssert.IsNotEmpty(result);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo("1"));
            Assert.That(first.Date, Is.EqualTo(new DateTime(2021, 1, 1)));
        });
    }
}