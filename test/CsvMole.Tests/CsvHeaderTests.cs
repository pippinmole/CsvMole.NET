namespace CsvMole.Tests;

public class CsvHeaderTests
{
    private static CsvColumnMap Run(string? header, string?[] names, int[]? orders = null)
    {
        orders ??= names.Select(_ => -1).ToArray();
        var members = names.Select((_, i) => "M" + i).ToArray();
        if (header is null) return CsvHeader.Resolve(null, names, orders, members);
        using var r = CsvReaderTests.Make(header, 64, 64);
        Assert.That(r.Read(), Is.True);
        return CsvHeader.Resolve(r, names, orders, members);
    }

    private static CsvError Fail(string? header, string?[] names, int[]? orders = null) =>
        Assert.Throws<CsvParseException>(() => Run(header, names, orders))!.Error;

    [Test]
    public void Resolve_ByName_Reordered()
    {
        var map = Run("b,a", ["a", "b"]);
        Assert.That(map.Indexes, Is.EqualTo(new[] { 1, 0 }));
        Assert.That(map.HeaderNames, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void Resolve_CaseInsensitiveTrimmed() =>
        Assert.That(Run(" ID ,Name", ["id"]).Indexes, Is.EqualTo(new[] { 0 }));

    [Test]
    public void Resolve_IgnoresLeadingBom()
    {
        var map = Run("﻿Id,Name", ["id"]);
        Assert.That(map.Indexes, Is.EqualTo(new[] { 0 }));
        Assert.That(map.HeaderNames, Is.EqualTo(new[] { "Id" }));
    }

    [Test]
    public void Resolve_OrderOnly_WithHeader()
    {
        var map = Run("x,y,z", [null], [2]);
        Assert.That(map.Indexes, Is.EqualTo(new[] { 2 }));
        Assert.That(map.HeaderNames, Is.EqualTo(new[] { "z" }));
        Assert.That(Run("x", [null], [5]).HeaderNames, Is.EqualTo(new string?[] { null }));
    }

    [Test]
    public void Resolve_NameWinsOverOrder() =>
        Assert.That(Run("a,b", ["b"], [0]).Indexes, Is.EqualTo(new[] { 1 }));

    [Test]
    public void Resolve_MissingName_NoFallback()
    {
        var ex = Assert.Throws<CsvParseException>(() => Run("a", ["b"], [0]))!;
        Assert.That(ex.Error.Reason, Is.EqualTo(CsvErrorReason.MissingHeaderColumn));
        Assert.That(ex.Error.HeaderName, Is.EqualTo("b"));
        Assert.That(ex.Message, Does.Contain("b"));
    }

    [Test]
    public void Resolve_DuplicateMappedHeader_Throws() =>
        Assert.That(Fail("a,A", ["a"]).Reason, Is.EqualTo(CsvErrorReason.DuplicateHeaderColumn));

    [Test]
    public void Resolve_DuplicateUnmappedHeader_Ignored() =>
        Assert.That(Run("x,x,a", ["a"]).Indexes, Is.EqualTo(new[] { 2 }));

    [Test]
    public void Resolve_NoHeader_NameOnly_Throws()
    {
        var e = Fail(null, ["a"]);
        Assert.That(e.Reason, Is.EqualTo(CsvErrorReason.UnresolvableColumn));
        Assert.That(e.MemberName, Is.EqualTo("M0"));
    }

    [Test]
    public void Resolve_NoHeader_Orders()
    {
        var map = Run(null, [null, null], [3, 0]);
        Assert.That(map.Indexes, Is.EqualTo(new[] { 3, 0 }));
        Assert.That(map.HeaderNames, Is.EqualTo(new string?[] { null, null }));
    }
}
