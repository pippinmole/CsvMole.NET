using CsvMole.Abstractions.Attributes;

namespace CsvMole.SourceGenerators.Tests;

public class CsvDelimiterAttributeTests
{
    [Test]
    public void CsvDelimiterAttribute_Constructor_WithPipeDelimiter_AssignsCorrectProperties()
    {
        // Arrange
        const string delimiter = "|";

        // Act
        var attribute = new CsvDelimiterAttribute(delimiter);

        // Assert
        Assert.That(attribute.Delimiter, Is.EqualTo(delimiter));
    }
    
    [Test]
    public void CsvDelimiterAttribute_Constructor_WithCommaDelimiter_AssignsCorrectProperties()
    {
        // Arrange
        const string delimiter = ",";

        // Act
        var attribute = new CsvDelimiterAttribute(delimiter);

        // Assert
        Assert.That(attribute.Delimiter, Is.EqualTo(delimiter));
    }
}