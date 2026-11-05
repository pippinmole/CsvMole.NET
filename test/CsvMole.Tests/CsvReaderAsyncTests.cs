namespace CsvMole.Tests;

using static CsvReaderTests;

public class CsvReaderAsyncTests
{
    private static async Task<List<string[]>> ReadAllAsync(string input, int size, int chunk, CsvOptions options)
    {
        using var reader = Make(input, size, chunk, options);
        var rows = new List<string[]>();
        while (await reader.ReadAsync())
        {
            var row = new string[reader.FieldCount];
            for (var i = 0; i < row.Length; i++) row[i] = reader[i].ToString();
            rows.Add(row);
        }
        return rows;
    }

    [TestCaseSource(typeof(ReaderCases), nameof(ReaderCases.All))]
    public async Task ReadAsync_MatchesRead(string input, CsvOptions options, string[][] expected)
    {
        foreach (var size in new[] { 4, 7, 16, 4096 })
            foreach (var chunk in new[] { 1, 2, 3, int.MaxValue })
            {
                var sync = ReadAll(input, size, chunk, options);
                Assert.That(sync, Is.EqualTo(expected), $"Read size={size} chunk={chunk}");
                Assert.That(await ReadAllAsync(input, size, chunk, options), Is.EqualTo(sync), $"size={size} chunk={chunk}");
            }
    }

    [Test]
    public async Task ReadAsync_LineNumbersMatch([Values(4, 4096)] int size, [Values(1, int.MaxValue)] int chunk)
    {
        using var reader = Make("a\n\n\"x\ny\",1\nz", size, chunk);
        var lines = new List<long>();
        while (await reader.ReadAsync()) lines.Add(reader.LineNumber);
        Assert.That(lines, Is.EqualTo(new[] { 1L, 3L, 5L }));
    }

    [Test]
    public async Task ReadAsync_Errors_MatchRead([Values(4, 4096)] int size, [Values(1, int.MaxValue)] int chunk)
    {
        var tooLong = await Assert.ThrowsAsync<CsvParseException>(() =>
            ReadAllAsync("abcdefghijk\nx", size, chunk, new CsvOptions { MaxRecordLength = 10 }));
        Assert.That(tooLong!.Error.Reason, Is.EqualTo(CsvErrorReason.RecordTooLong));
        var unterminated = await Assert.ThrowsAsync<CsvParseException>(() => ReadAllAsync("a\n\"bc", size, chunk, new CsvOptions()));
        Assert.That(unterminated!.Error.Reason, Is.EqualTo(CsvErrorReason.UnterminatedQuote));
        Assert.That(unterminated.Error.LineNumber, Is.EqualTo(2));
    }

    [Test]
    public async Task ReadAsync_Cancelled_Throws()
    {
        using var reader = Make("a,b\nc,d", 16, 2);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await reader.ReadAsync(cts.Token));
    }

    [Test]
    public void ReadAsync_BufferedRecord_CompletesSynchronously()
    {
        using var reader = Make("a\nb\nc", 4096, int.MaxValue);
        Assert.That(reader.Read(), Is.True); // fills the buffer
        var task = reader.ReadAsync();
        Assert.That(task.IsCompletedSuccessfully, Is.True);
        Assert.That(task.Result, Is.True);
    }

    [Test]
    public async Task ReadAsync_ErrorInBufferedData_FaultsTask()
    {
        using var reader = Make("a\nabcdefghijk\nx", 4096, int.MaxValue, new CsvOptions { MaxRecordLength = 10 });
        Assert.That(await reader.ReadAsync(), Is.True); // buffers everything
        ValueTask<bool> task = default;
        Assert.DoesNotThrow(() => task = reader.ReadAsync());
        Assert.That(task.IsFaulted, Is.True);
        var ex = Assert.Throws<CsvParseException>(() => _ = task.Result);
        Assert.That(ex!.Error.Reason, Is.EqualTo(CsvErrorReason.RecordTooLong));
    }
}
