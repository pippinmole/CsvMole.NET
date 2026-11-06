namespace CsvMole.Tests;

public class CsvOptionsTests
{
    [Test]
    public void NullCulture_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _ = new CsvOptions { Culture = null! });

    [TestCase(0)]
    [TestCase(-1)]
    public void NonPositiveMaxRecordLength_Throws(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new CsvOptions { MaxRecordLength = value });
}
