namespace CsvMole.SourceGenerators.Tests;

public class SnapshotTests
{
    private const string Header = """
        using System;
        using System.Collections.Generic;
        using System.Globalization;
        using System.IO;
        using System.Threading;
        using CsvMole;

        """;

    private static void Check(
        string source,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "",
        [System.Runtime.CompilerServices.CallerFilePath] string file = "")
    {
        var (gen, comp, driver) = GeneratorHarness.Run(Header + source);
        Assert.That(gen, Is.Empty, string.Join(Environment.NewLine, gen));
        Assert.That(comp, Is.Empty, string.Join(Environment.NewLine, comp));
        var sources = driver.GetRunResult().Results[0].GeneratedSources;
        Assert.That(sources, Has.Length.EqualTo(1));
        Snapshot.Match(sources[0].SourceText.ToString(), name, file);
    }

    [Test]
    public void MutableClass() => Check("""
        public class Person
        {
            [CsvColumn("name")] public string Name { get; set; } = "";
            [CsvColumn("age")] public int Age { get; set; }
        }

        [CsvParser]
        public partial class P { public partial IEnumerable<Person> Parse(TextReader reader, CsvOptions? options = null); }
        """);

    [Test]
    public void Record() => Check("""
        public record Person([CsvColumn("name")] string Name, [CsvColumn("age")] int Age);

        [CsvParser]
        public partial class P { public partial IEnumerable<Person> Parse(TextReader reader, CsvOptions? options = null); }
        """);

    [Test]
    public void CtorPlusInitializer() => Check("""
        public class Person
        {
            public Person([CsvColumn("name")] string name) { Name = name; }
            public string Name { get; }
            [CsvColumn("age")] public int Age { get; init; }
        }

        [CsvParser]
        public partial class P { public partial IEnumerable<Person> Parse(TextReader reader, CsvOptions? options = null); }
        """);

    [Test]
    public void RequiredMembers() => Check("""
        public class Person
        {
            [CsvColumn("name")] public required string Name { get; set; }
            [CsvColumn("age")] public required int Age { get; init; }
        }

        [CsvParser]
        public partial class P { public partial IEnumerable<Person> Parse(TextReader reader, CsvOptions? options = null); }
        """);

    [Test]
    public void NestedParser() => Check("""
        public class Person { [CsvColumn("name")] public string Name { get; set; } = ""; }

        namespace My.App
        {
            public static partial class Outer
            {
                [CsvParser]
                internal partial class P { public partial IEnumerable<Person> Parse(TextReader reader, CsvOptions? options = null); }
            }
        }
        """);

    [Test]
    public void Converter() => Check("""
        public sealed class UpperConverter : CsvConverter<string>
        {
            public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out string result)
            {
                result = value.ToString().ToUpperInvariant();
                return true;
            }
        }

        public class Person
        {
            [CsvColumn("name"), CsvConverter(typeof(UpperConverter))] public string Name { get; set; } = "";
        }

        [CsvParser]
        public partial class P { public partial IEnumerable<Person> Parse(TextReader reader, CsvOptions? options = null); }
        """);

    [Test]
    public void NullableAndEnum() => Check("""
        public enum Color { Red, Green }

        public class Row
        {
            [CsvColumn("a")] public int? A { get; set; }
            [CsvColumn("b")] public string? B { get; set; }
            [CsvColumn("c")] public Color C { get; set; }
            [CsvColumn("d")] public Color? D { get; set; }
            [CsvColumn("e")] public DateTime E { get; set; }
            [CsvColumn("f")] public decimal? F { get; set; }
        }

        [CsvParser]
        public partial class P { public partial IEnumerable<Row> Parse(TextReader reader, CsvOptions? options = null); }
        """);

    [Test]
    public void SyncAndAsyncSameModel()
    {
        const string source = """
            public class Person { [CsvColumn("name")] public string Name { get; set; } = ""; }

            [CsvParser]
            public partial class P
            {
                public partial IEnumerable<Person> Parse(TextReader reader, CsvOptions? options = null);
                public partial IAsyncEnumerable<Person> ParseAsync(TextReader reader, CsvOptions? options = null, CancellationToken cancellationToken = default);
            }
            """;
        var (_, _, driver) = GeneratorHarness.Run(Header + source);
        var text = driver.GetRunResult().Results[0].GeneratedSources[0].SourceText.ToString();
        Assert.That(System.Text.RegularExpressions.Regex.Matches(text, @"bool __TryBind\w*\(").Count, Is.EqualTo(1));
        Check(source);
    }

    [Test]
    public void TwoConvertersSameShortName() => Check("""
        namespace A { public sealed class DateConverter : CsvConverter<DateTime> { public override bool TryParse(ReadOnlySpan<char> v, CultureInfo c, out DateTime r) => DateTime.TryParse(v, c, out r); } }
        namespace B { public sealed class DateConverter : CsvConverter<DateTime> { public override bool TryParse(ReadOnlySpan<char> v, CultureInfo c, out DateTime r) => DateTime.TryParseExact(v, "yyyyMMdd", c, DateTimeStyles.None, out r); } }

        public class Row
        {
            [CsvColumn("a"), CsvConverter(typeof(A.DateConverter))] public DateTime A { get; set; }
            [CsvColumn("b"), CsvConverter(typeof(B.DateConverter))] public DateTime B { get; set; }
        }

        [CsvParser]
        public partial class P { public partial IEnumerable<Row> Parse(TextReader reader, CsvOptions? options = null); }
        """);
}
