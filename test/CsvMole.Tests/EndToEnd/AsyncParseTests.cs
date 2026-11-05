namespace CsvMole.Tests.EndToEnd;

public class AsyncParseTests
{
    private static readonly EndToEndParser Parser = new();

    [Test]
    public async Task Async_MatchesSync()
    {
        const string csv = "Id,Age\n1,2\n3,4";
        var sync = Parser.ParseAged(new StringReader(csv)).ToList();
        var asyncRows = new List<Aged>();
        await foreach (var m in Parser.ParseAgedAsync(new StringReader(csv))) asyncRows.Add(m);
        Assert.That(asyncRows.Select(a => (a.Id, a.Age)), Is.EqualTo(sync.Select(a => (a.Id, a.Age))));
    }

    [Test]
    public async Task Async_NoHeader_SharedModel()
    {
        var rows = new List<Pair>();
        await foreach (var m in Parser.ParsePairsAsync(new StringReader("1,2"), new CsvOptions { HasHeader = false })) rows.Add(m);
        Assert.That(rows.Single().B, Is.EqualTo(2));
    }

    [Test]
    public async Task Async_WithCancellation_Extension()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in Parser.ParseAgedAsync(new StringReader("Id,Age\n1,2")).WithCancellation(cts.Token)) { }
        });
    }

    [Test]
    public async Task Async_TokenParameter()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in Parser.ParseAgedAsync(new StringReader("Id,Age\n1,2"), null, cts.Token)) { }
        });
    }
}
