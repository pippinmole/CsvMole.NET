using System.Globalization;

namespace CsvMole.Tests.EndToEnd;

public class LifecycleTests
{
    private static readonly EndToEndParser Parser = new();

    [Test]
    public void BreakEarly_DoesNotDisposeCallerReader()
    {
        var sr = new StringReader("Id,Age\n1,2\n3,4");
        Parser.ParseAged(sr).First();
        Assert.DoesNotThrow(() => sr.ReadToEnd());
    }

    [Test]
    public void Default_IgnoresCurrentCulture()
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.That(Parser.ParseMoney(new StringReader("Amount\n1.5")).Single().Amount, Is.EqualTo(1.5m));
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Test]
    public void CultureOption() =>
        Assert.That(Parser.ParseSemiMoney(new StringReader("Amount\n1,5"),
            new CsvOptions { Culture = new CultureInfo("de-DE") }).Single().Amount, Is.EqualTo(1.5m));
}
