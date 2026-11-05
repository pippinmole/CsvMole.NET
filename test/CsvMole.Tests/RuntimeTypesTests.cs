using System.Globalization;

namespace CsvMole.Tests;

public class RuntimeTypesTests
{
    private static CsvError Err(CsvErrorReason r = CsvErrorReason.ParseFailed) => new(7, 2, "Age", "Age", "abc", r);

    [Test]
    public void Options_Defaults()
    {
        var o = CsvOptions.Default;
        Assert.That(o.HasHeader, Is.True);
        Assert.That(o.Delimiter, Is.Null);
        Assert.That(o.Culture, Is.SameAs(CultureInfo.InvariantCulture));
        Assert.That(o.SkipBlankLines, Is.True);
        Assert.That(o.TrimFields, Is.False);
        Assert.That(o.OnError, Is.Null);
        Assert.That(o.MaxRecordLength, Is.EqualTo(1048576));
    }

    [Test]
    public void Exception_MessageContainsDetails()
    {
        var m = new CsvParseException(Err()).Message;
        Assert.That(m, Does.Contain("line 7").And.Contain("column 2").And.Contain("Age")
            .And.Contain("abc").And.Contain("ParseFailed"));
    }

    [Test]
    public void Report_NoCallback_Throws() =>
        Assert.Throws<CsvParseException>(() => CsvErrors.Report(CsvOptions.Default, Err()));

    [Test]
    public void Report_WithCallback_InvokesAndReturns()
    {
        CsvError? got = null;
        var e = Err();
        CsvErrors.Report(new CsvOptions { OnError = x => got = x }, e);
        Assert.That(got, Is.SameAs(e));
    }

    [Test]
    public void Report_UnterminatedQuote_ThrowsEvenWithCallback() =>
        Assert.Throws<CsvParseException>(() =>
            CsvErrors.Report(new CsvOptions { OnError = _ => { } }, Err(CsvErrorReason.UnterminatedQuote)));

    [Test]
    public void Truncate_LongValue()
    {
        var t = CsvErrors.Truncate(new string('x', 500));
        Assert.That(t, Has.Length.EqualTo(201));
        Assert.That(t[^1], Is.EqualTo('…'));
    }

    [Test]
    public void ColumnMap_RoundTrips()
    {
        var m = new CsvColumnMap([1, 0], ["B", null]);
        Assert.That(m.Indexes, Is.EqualTo(new[] { 1, 0 }));
        Assert.That(m.HeaderNames, Is.EqualTo(new string?[] { "B", null }));
    }
}
