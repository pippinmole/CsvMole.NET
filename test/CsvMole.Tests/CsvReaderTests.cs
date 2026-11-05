namespace CsvMole.Tests;

public class CsvReaderTests
{
    private static readonly CsvOptions Defaults = new();

    // Returns at most maxChunk chars per Read, so records, CRLFs and blank lines genuinely span refills.
    internal sealed class ChunkedReader(string text, int maxChunk) : StringReader(text)
    {
        public override int Read(char[] buffer, int index, int count) =>
            base.Read(buffer, index, Math.Min(count, maxChunk));
        public override int Read(Span<char> buffer) => base.Read(buffer[..Math.Min(buffer.Length, maxChunk)]);
    }

    internal static CsvReader Make(string input, int size, int chunk, CsvOptions? options = null, char delimiter = ',') =>
        new(new ChunkedReader(input, chunk), options ?? Defaults, delimiter, size);

    internal static List<string[]> ReadAll(string input, int bufferSize, int chunk, CsvOptions? options = null, char delimiter = ',')
    {
        using var reader = Make(input, bufferSize, chunk, options, delimiter);
        var rows = new List<string[]>();
        while (reader.Read())
        {
            var row = new string[reader.FieldCount];
            for (var i = 0; i < row.Length; i++) row[i] = reader[i].ToString();
            rows.Add(row);
        }
        return rows;
    }

    [Test]
    public void SimpleRows([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("a,b\nc,d", size, chunk), Is.EqualTo(new[] { new[] { "a", "b" }, new[] { "c", "d" } }));

    [Test]
    public void LineEndings([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("a\r\nb\rc\nd", size, chunk),
            Is.EqualTo(new[] { new[] { "a" }, new[] { "b" }, new[] { "c" }, new[] { "d" } }));

    [Test]
    public void TrailingNewline_NoExtraRecord([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("a\n", size, chunk), Is.EqualTo(new[] { new[] { "a" } }));

    [Test]
    public void EmptyInput([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk)
    {
        using var reader = Make("", size, chunk);
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void EmptyFields([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("a,,c,", size, chunk), Is.EqualTo(new[] { new[] { "a", "", "c", "" } }));

    [Test]
    public void BlankLines_SkippedByDefault([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk)
    {
        using var reader = Make("a\n\n\nb", size, chunk);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader[0].ToString(), Is.EqualTo("a"));
        Assert.That(reader.LineNumber, Is.EqualTo(1));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader[0].ToString(), Is.EqualTo("b"));
        Assert.That(reader.LineNumber, Is.EqualTo(4));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void BlankLines_KeptWhenDisabled([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk)
    {
        var rows = ReadAll("a\n\nb", size, chunk, new CsvOptions { SkipBlankLines = false });
        Assert.That(rows, Is.EqualTo(new[] { new[] { "a" }, new[] { "" }, new[] { "b" } }));
    }

    [Test]
    public void CustomDelimiter([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("a;b,c", size, chunk, delimiter: ';'), Is.EqualTo(new[] { new[] { "a", "b,c" } }));

    [Test]
    public void CrLfSplitAcrossBuffer([Values(1, 2, 4)] int chunk)
    {
        // chunk 4: the first read is "abc\r", so \r sits at the buffer end and \n arrives in the next read.
        Assert.That(ReadAll("abc\r\ndef", 4, chunk), Is.EqualTo(new[] { new[] { "abc" }, new[] { "def" } }));
        Assert.That(ReadAll("abc\r", 4, chunk), Is.EqualTo(new[] { new[] { "abc" } }));
        Assert.That(ReadAll("abc\r\r\ndef", 4, chunk, new CsvOptions { SkipBlankLines = false }),
            Is.EqualTo(new[] { new[] { "abc" }, new[] { "" }, new[] { "def" } }));
    }

    [Test]
    public void RecordSpanningManyRefills([Values(1, 3)] int chunk) =>
        Assert.That(ReadAll("abcdefghijklmnopqrstuvwxyz,0123456789abcdefghij\r\nx,y", 4, chunk),
            Is.EqualTo(new[] { new[] { "abcdefghijklmnopqrstuvwxyz", "0123456789abcdefghij" }, new[] { "x", "y" } }));

    [Test]
    public void ManyShortRecords_ShiftTailInsteadOfGrowing([Values(1, 3, int.MaxValue)] int chunk)
    {
        var input = string.Concat(Enumerable.Range(0, 1000).Select(i => $"r{i},x\r\n"));
        using var reader = Make(input, 16, chunk);
        var initial = reader.BufferLength;
        var count = 0;
        while (reader.Read())
        {
            Assert.That(reader[0].ToString(), Is.EqualTo("r" + count));
            Assert.That(reader.LineNumber, Is.EqualTo(count + 1));
            count++;
        }
        Assert.That(count, Is.EqualTo(1000));
        Assert.That(reader.BufferLength, Is.EqualTo(initial));
    }

    [Test]
    public void TrimFields_WhitespaceOnlyLine_IsEmptyRecord([Values(4, 4096)] int size, [Values(1, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("a\n  \nb", size, chunk, new CsvOptions { TrimFields = true }),
            Is.EqualTo(new[] { new[] { "a" }, new[] { "" }, new[] { "b" } }));

    [Test]
    public void WideRow_1000Columns([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk)
    {
        var line = string.Join(",", Enumerable.Range(0, 1000).Select(i => "f" + i));
        using var reader = Make(line, size, chunk);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.FieldCount, Is.EqualTo(1000));
        Assert.That(reader[999].ToString(), Is.EqualTo("f999"));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void RecordTooLong([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk)
    {
        var ex = Assert.Throws<CsvParseException>(() =>
            ReadAll("abcdefghijk\nx", size, chunk, new CsvOptions { MaxRecordLength = 10 }));
        Assert.That(ex!.Error.Reason, Is.EqualTo(CsvErrorReason.RecordTooLong));
    }

    [Test]
    public void RecordAtMaxLength_Allowed([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll("abcdefghij\r\nx", size, chunk, new CsvOptions { MaxRecordLength = 10 }),
            Is.EqualTo(new[] { new[] { "abcdefghij" }, new[] { "x" } }));

    [Test]
    public void TrimFields([Values(4, 7, 16, 4096)] int size, [Values(1, 2, 3, int.MaxValue)] int chunk) =>
        Assert.That(ReadAll(" a , b ", size, chunk, new CsvOptions { TrimFields = true }),
            Is.EqualTo(new[] { new[] { "a", "b" } }));

    [Test]
    public void Indexer_OutOfRange_Throws()
    {
        using var reader = new CsvReader(new StringReader("a,b"), Defaults, ',');
        reader.Read();
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = reader[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = reader[-1]);
    }

    [Test]
    public void Dispose_DoesNotDisposeTextReader()
    {
        var text = new StringReader("a,b");
        var reader = new CsvReader(text, Defaults, ',');
        reader.Read();
        reader.Dispose();
        Assert.DoesNotThrow(() => text.Peek());
    }
}
