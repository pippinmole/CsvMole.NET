namespace CsvMole.Tests.EndToEnd;

public class SyncParseTests
{
    private static readonly EndToEndParser Parser = new();
    private static readonly CsvOptions NoHeader = new() { HasHeader = false };

    private static StringReader R(string s) => new(s);

    [Test]
    public void MutableClass_ByOrder()
    {
        var m = Parser.ParseMutable(R("1,2024-01-02,x"), NoHeader).Single();
        Assert.That(m.Id, Is.EqualTo("1"));
        Assert.That(m.Date, Is.EqualTo(new DateTime(2024, 1, 2)));
        Assert.That(m.Desc, Is.EqualTo("x"));
    }

    [Test]
    public void SameShortNameModels_DoNotClash() =>
        Assert.That(Parser.ParseOtherMutable(R("42"), NoHeader).Single().Value, Is.EqualTo(42));

    [Test]
    public void Record_ByHeader_Reordered() =>
        Assert.That(Parser.ParsePeople(R("born,name\n1990-05-06,Ann")).Single(),
            Is.EqualTo(new Person("Ann", new DateOnly(1990, 5, 6))));

    [Test]
    public void CtorPlusInitAndRequired()
    {
        var m = Parser.ParseCtor(R("7,Bob,C1"), NoHeader).Single();
        Assert.That(m.Id, Is.EqualTo(7));
        Assert.That(m.Name, Is.EqualTo("Bob"));
        Assert.That(m.Code, Is.EqualTo("C1"));
    }

    [Test]
    public void AllBuiltIns()
    {
        var m = Parser.ParseAll(R(
            "true,c,255,-3,42,9000000000,1.5,2.25,3.75,2024-01-02T03:04:05+01:00,13:14:15,01:02:03,"
            + "3f2504e0-4f89-11d3-9a0c-0305e82c3301,red"), NoHeader).Single();
        Assert.Multiple(() =>
        {
            Assert.That(m.Bool, Is.True);
            Assert.That(m.Char, Is.EqualTo('c'));
            Assert.That(m.Byte, Is.EqualTo(255));
            Assert.That(m.Short, Is.EqualTo(-3));
            Assert.That(m.Int, Is.EqualTo(42));
            Assert.That(m.Long, Is.EqualTo(9000000000L));
            Assert.That(m.Float, Is.EqualTo(1.5f));
            Assert.That(m.Double, Is.EqualTo(2.25));
            Assert.That(m.Decimal, Is.EqualTo(3.75m));
            Assert.That(m.DateTimeOffset, Is.EqualTo(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(1))));
            Assert.That(m.TimeOnly, Is.EqualTo(new TimeOnly(13, 14, 15)));
            Assert.That(m.TimeSpan, Is.EqualTo(new TimeSpan(1, 2, 3)));
            Assert.That(m.Guid, Is.EqualTo(Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301")));
            Assert.That(m.Color, Is.EqualTo(Color.Red));
        });
    }

    [Test]
    public void Nullables()
    {
        var m = Parser.ParseNullables(R(",,,"), NoHeader).Single();
        Assert.That(m.Number, Is.Null);
        Assert.That(m.Maybe, Is.Null);
        Assert.That(m.Text, Is.EqualTo(""));
        Assert.That(m.Color, Is.Null);

        var n = Parser.ParseNullables(R("5,s,t,green"), NoHeader).Single();
        Assert.That((n.Number, n.Maybe, n.Text, n.Color), Is.EqualTo(((int?)5, "s", "t", (Color?)Color.Green)));
    }

    [Test]
    public void Converter() =>
        Assert.That(Parser.ParseConverted(R("x5"), NoHeader).Single().Value, Is.EqualTo(5));

    [Test]
    public void DelimiterAttribute_And_OptionOverride()
    {
        var a = Parser.ParseSemi(R("a;b"), NoHeader).Single();
        Assert.That((a.A, a.B), Is.EqualTo(("a", "b")));
        var b = Parser.ParseSemi(R("a,b"), new CsvOptions { HasHeader = false, Delimiter = ',' }).Single();
        Assert.That((b.A, b.B), Is.EqualTo(("a", "b")));
    }

    [Test]
    public void KeywordCtorParam_OptionalOmitted_TabDelimiter()
    {
        var m = Parser.ParseKeyword(R("v\tignored"), NoHeader).Single();
        Assert.That((m.Class, m.Skipped), Is.EqualTo(("v", 7)));
    }

    [Test]
    public void TextReaderSubclassParameter()
    {
        var rows = Parser.ParsePairsFromString(R("1,2\n3,4"), NoHeader).ToList();
        Assert.That(rows.Select(p => (p.A, p.B)), Is.EqualTo(new[] { (1, 2), (3, 4) }));
    }

    [Test]
    public void ExtraColumnsIgnored()
    {
        var m = Parser.ParsePairs(R("1,2,3,4"), NoHeader).Single();
        Assert.That((m.A, m.B), Is.EqualTo((1, 2)));
    }

    [Test]
    public void NoOptionsParameter_UsesDefault_WithHeader() =>
        Assert.That(Parser.ParsePairsDefault(R("a,b\n1,2")).Single().B, Is.EqualTo(2));

    [Test]
    public void EmptyInputWithHeader_Empty() => Assert.That(Parser.ParseNamed(R("")), Is.Empty);

    [Test]
    public void HeaderOnly_Empty() => Assert.That(Parser.ParseNamed(R("Id,Name")), Is.Empty);

    [Test]
    public void ParseFailed_Throws_WithDetails()
    {
        var e = Assert.Throws<CsvParseException>(() => Parser.ParseNamed(R("Id,Name\n1,a\nzz,b")).ToList())!.Error;
        Assert.That(e, Is.EqualTo(new CsvError(3, 0, "Id", "Id", "zz", CsvErrorReason.ParseFailed)));
    }

    [Test]
    public void TooFewColumns_ReportedAndSkipped_WhenOnError()
    {
        var errors = new List<CsvError>();
        var rows = Parser.ParsePairs(R("1\n3,4"), new CsvOptions { HasHeader = false, OnError = errors.Add }).ToList();
        Assert.That(rows.Select(p => p.A), Is.EqualTo(new[] { 3 }));
        Assert.That(errors, Is.EqualTo(new[] { new CsvError(1, 1, null, "B", null, CsvErrorReason.TooFewColumns) }));
    }

    [Test]
    public void TooFewColumns_NullableMembersBecomeNull()
    {
        // Column 3 (Color?) is missing entirely.
        var m = Parser.ParseNullables(R("5,,t"), NoHeader).Single();
        Assert.That((m.Number, m.Text, m.Color), Is.EqualTo(((int?)5, "t", (Color?)null)));
    }

    [Test]
    public void TooFewColumns_NullableConverterMemberBecomesNull()
    {
        var errors = new List<CsvError>();
        var m = Parser.ParseConvertedNullable(R("1"), new CsvOptions { HasHeader = false, OnError = errors.Add }).Single();
        Assert.That((m.A, m.B), Is.EqualTo((1, (int?)null)));
        Assert.That(errors, Is.Empty);
        // A present but empty field still goes to the converter.
        Assert.That(Parser.ParseConvertedNullable(R("1,"), NoHeader).Single().B, Is.EqualTo(-1));
    }
}
