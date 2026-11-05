namespace CsvMole.Tests.EndToEnd;

public class ErrorPolicyTests
{
    private static readonly EndToEndParser Parser = new();
    private static readonly CsvOptions NoHeader = new() { HasHeader = false };

    [Test]
    public void ParseFailure_Throws_WithContext()
    {
        var e = Assert.Throws<CsvParseException>(() => Parser.ParseAged(new StringReader("Id,Age\n1,abc")).ToList())!.Error;
        Assert.Multiple(() =>
        {
            Assert.That(e.LineNumber, Is.EqualTo(2));
            Assert.That(e.ColumnIndex, Is.EqualTo(1));
            Assert.That(e.HeaderName, Is.EqualTo("Age"));
            Assert.That(e.MemberName, Is.EqualTo("Age"));
            Assert.That(e.RawValue, Is.EqualTo("abc"));
            Assert.That(e.Reason, Is.EqualTo(CsvErrorReason.ParseFailed));
        });
    }

    [Test]
    public void ParseFailure_Callback_SkipsRow()
    {
        var errors = new List<CsvError>();
        var rows = Parser.ParseAged(new StringReader("Id,Age\n1,2\n3,x\n5,6"), new CsvOptions { OnError = errors.Add }).ToList();
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void TooFewColumns_NonNullable_Error()
    {
        var e = Assert.Throws<CsvParseException>(() => Parser.ParsePairs(new StringReader("1"), NoHeader).ToList())!.Error;
        Assert.That(e.Reason, Is.EqualTo(CsvErrorReason.TooFewColumns));
    }

    [Test]
    public void TooFewColumns_AllMissingNullable_Null()
    {
        var m = Parser.ParseOptPairs(new StringReader("5"), NoHeader).ToList();
        Assert.That(m, Has.Count.EqualTo(1));
        Assert.Multiple(() => { Assert.That(m[0].A, Is.EqualTo(5)); Assert.That(m[0].B, Is.Null); Assert.That(m[0].C, Is.Null); });
    }

    [Test]
    public void MissingHeaderColumn_ThrowsBeforeRows()
    {
        var errors = new List<CsvError>();
        var yielded = 0;
        var ex = Assert.Throws<CsvParseException>(() =>
        {
            foreach (var _ in Parser.ParseAged(new StringReader("Id,Other\n1,2"), new CsvOptions { OnError = errors.Add })) yielded++;
        })!;
        Assert.That(ex.Error.Reason, Is.EqualTo(CsvErrorReason.MissingHeaderColumn));
        Assert.That(yielded, Is.EqualTo(0));
        Assert.That(errors, Is.Empty);
    }
}
