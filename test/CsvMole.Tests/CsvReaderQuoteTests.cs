namespace CsvMole.Tests;

using static CsvReaderTests;

public class CsvReaderQuoteTests
{
    private static readonly CsvOptions Trim = new() { TrimFields = true };

    [Test]
    public void QuotedDelimiter([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("\"a,b\",c", size, chunk), Is.EqualTo(new[] { new[] { "a,b", "c" } }));

    [Test]
    public void EscapedQuote([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("\"say \"\"hi\"\"\"", size, chunk), Is.EqualTo(new[] { new[] { "say \"hi\"" } }));

    [Test]
    public void NewlineInQuotes_LineNumbers([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk)
    {
        using var reader = Make("\"x\ny\",1\nz,2", size, chunk);
        Assert.That(reader.Read(), Is.True);
        Assert.That(new[] { reader[0].ToString(), reader[1].ToString() }, Is.EqualTo(new[] { "x\ny", "1" }));
        Assert.That(reader.LineNumber, Is.EqualTo(1));
        Assert.That(reader.Read(), Is.True);
        Assert.That(new[] { reader[0].ToString(), reader[1].ToString() }, Is.EqualTo(new[] { "z", "2" }));
        Assert.That(reader.LineNumber, Is.EqualTo(3));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void EmptyQuoted([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("\"\",a", size, chunk), Is.EqualTo(new[] { new[] { "", "a" } }));

    [Test]
    public void LiteralQuoteMidField([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("ab\"c,d", size, chunk), Is.EqualTo(new[] { new[] { "ab\"c", "d" } }));

    [Test]
    public void UnterminatedQuote_Throws([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk)
    {
        var ex = Assert.Throws<CsvParseException>(() => ReadAll("a\n\"bc", size, chunk));
        Assert.That(ex!.Error.Reason, Is.EqualTo(CsvErrorReason.UnterminatedQuote));
        Assert.That(ex.Error.LineNumber, Is.EqualTo(2));
    }

    [Test]
    public void Trim_QuoteAfterSpaces([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("a,  \"b,c\"", size, chunk, Trim), Is.EqualTo(new[] { new[] { "a", "b,c" } }));

    [Test]
    public void NoTrim_QuoteAfterSpacesIsLiteral([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("a,  \"b,c\"", size, chunk), Is.EqualTo(new[] { new[] { "a", "  \"b", "c\"" } }));

    [Test]
    public void Trim_NotInsideQuotes([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("\" a \"", size, chunk, Trim), Is.EqualTo(new[] { new[] { " a " } }));

    // Beyond the brief: boundary and edge cases of the same rules.

    [Test]
    public void SeveralEscapedFields_MixedWithPlain([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("\"\"\"\",x,\"a\"\"b\"\r\n\"c\"\"\",\"d\"", size, chunk),
            Is.EqualTo(new[] { new[] { "\"", "x", "a\"b" }, new[] { "c\"", "d" } }));

    [Test]
    public void CrLfAndCrInQuotes_CountLines([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk)
    {
        using var reader = Make("\"a\r\nb\rc\",1\r\nz", size, chunk);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader[0].ToString(), Is.EqualTo("a\r\nb\rc"));
        Assert.That(reader.LineNumber, Is.EqualTo(1));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.LineNumber, Is.EqualTo(4));
    }

    [Test]
    public void TrailingSpacesAfterClosingQuote_TrimmedWhenTrimming([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll(" \"a\"  ,b", size, chunk, Trim), Is.EqualTo(new[] { new[] { "a", "b" } }));

    [Test]
    public void TextAfterClosingQuote_FieldKeptRaw([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("\"a,b\"x,c", size, chunk), Is.EqualTo(new[] { new[] { "\"a,b\"x", "c" } }));

    [Test]
    public void UnterminatedQuote_AfterNewlineInQuotes_ReportsStartLine([Values(4, 4096)] int size, [Values(1, int.MaxValue)] int chunk)
    {
        var ex = Assert.Throws<CsvParseException>(() => ReadAll("a\nb,\"c\nd\ne", size, chunk));
        Assert.That(ex!.Error.Reason, Is.EqualTo(CsvErrorReason.UnterminatedQuote));
        Assert.That(ex.Error.LineNumber, Is.EqualTo(2));
    }

    [Test]
    public void MaxRecordLength_AppliesAcrossQuotedNewlines([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk)
    {
        var options = new CsvOptions { MaxRecordLength = 10 };
        Assert.That(ReadAll("\"ab\ncdef\"\nx", size, chunk, options), Is.EqualTo(new[] { new[] { "ab\ncdef" }, new[] { "x" } }));
        var ex = Assert.Throws<CsvParseException>(() => ReadAll("\"ab\ncdefgh\"\nx", size, chunk, options));
        Assert.That(ex!.Error.Reason, Is.EqualTo(CsvErrorReason.RecordTooLong));
    }

    [Test]
    public void LongTrickledQuotedRecord([Values(1, 3)] int chunk)
    {
        var body = string.Concat(Enumerable.Range(0, 500).Select(i => $"v{i}\"\",\n"));
        var input = $"\"{body}\",end\nnext";
        var rows = ReadAll(input, 4, chunk);
        Assert.That(rows, Is.EqualTo(new[] { new[] { body.Replace("\"\"", "\""), "end" }, new[] { "next" } }));
    }
}
