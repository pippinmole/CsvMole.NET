using System.Globalization;

namespace CsvMole.Tests.EndToEnd
{
    public sealed class MutableModel
    {
        [CsvOrder(0)] public string Id { get; set; } = "";
        [CsvOrder(1)] public DateTime Date { get; set; }
        [CsvOrder(2)] public string? Desc { get; set; }
    }

    public sealed record Person([property: CsvColumn("name")] string Name, [property: CsvColumn("born")] DateOnly Born);

    public sealed class CtorModel(int id, string code)
    {
        [CsvOrder(0)] public int Id { get; } = id;
        [CsvOrder(1)] public required string Name { get; init; }
        // Required and matched to the ctor parameter: still set in the initializer (ruling Q).
        [CsvOrder(2)] public required string Code { get; init; } = code;
    }

    public enum Color { Red, Green }

    public sealed class AllBuiltIns
    {
        [CsvOrder(0)] public bool Bool { get; set; }
        [CsvOrder(1)] public char Char { get; set; }
        [CsvOrder(2)] public byte Byte { get; set; }
        [CsvOrder(3)] public short Short { get; set; }
        [CsvOrder(4)] public int Int { get; set; }
        [CsvOrder(5)] public long Long { get; set; }
        [CsvOrder(6)] public float Float { get; set; }
        [CsvOrder(7)] public double Double { get; set; }
        [CsvOrder(8)] public decimal Decimal { get; set; }
        [CsvOrder(9)] public DateTimeOffset DateTimeOffset { get; set; }
        [CsvOrder(10)] public TimeOnly TimeOnly { get; set; }
        [CsvOrder(11)] public TimeSpan TimeSpan { get; set; }
        [CsvOrder(12)] public Guid Guid { get; set; }
        [CsvOrder(13)] public Color Color { get; set; }
    }

    public sealed class Nullables
    {
        [CsvOrder(0)] public int? Number { get; set; }
        [CsvOrder(1)] public string? Maybe { get; set; }
        [CsvOrder(2)] public string Text { get; set; } = "unset";
        [CsvOrder(3)] public Color? Color { get; set; }
    }

    public sealed class X5Converter : CsvConverter<int>
    {
        public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out int result)
        {
            result = 0;
            return value.StartsWith("x") && int.TryParse(value[1..], culture, out result);
        }
    }

    public sealed class Converted
    {
        [CsvOrder(0), CsvConverter(typeof(X5Converter))] public int Value { get; set; }
    }

    // Maps "" to -1 so a test can tell "converter saw an empty field" from "column missing".
    public sealed class NullableIntConverter : CsvConverter<int?>
    {
        public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out int? result)
        {
            result = value.IsEmpty ? -1 : int.Parse(value, culture);
            return true;
        }
    }

    public sealed class ConvertedNullable
    {
        [CsvOrder(0)] public int A { get; set; }
        [CsvOrder(1), CsvConverter(typeof(NullableIntConverter))] public int? B { get; set; }
    }

    [CsvDelimiter(';')]
    public sealed class Semi
    {
        [CsvOrder(0)] public string A { get; set; } = "";
        [CsvOrder(1)] public string B { get; set; } = "";
    }

    public sealed class Pair
    {
        [CsvOrder(0)] public int A { get; set; }
        [CsvOrder(1)] public int B { get; set; }
    }

    public sealed class Named
    {
        [CsvColumn("Id")] public int Id { get; set; }
        [CsvColumn("Name")] public string Name { get; set; } = "";
    }

    // Keyword ctor parameter name, unmapped optional parameter (omitted), tab delimiter literal.
    [CsvDelimiter('\t')]
    public sealed class KeywordModel
    {
        public KeywordModel([CsvOrder(0)] string @class, int skipped = 7)
        {
            Class = @class;
            Skipped = skipped;
        }

        public string Class { get; }
        public int Skipped { get; }
    }

    public sealed class Aged
    {
        [CsvColumn("Id")] public int Id { get; set; }
        [CsvColumn("Age")] public int Age { get; set; }
    }

    public sealed class OptPair
    {
        [CsvOrder(0)] public int? A { get; set; }
        [CsvOrder(1)] public string? B { get; set; }
        [CsvOrder(2)] public int? C { get; set; }
    }

    public sealed class Money
    {
        [CsvColumn("Amount")] public decimal Amount { get; set; }
    }

    [CsvDelimiter(';')]
    public sealed class SemiMoney
    {
        [CsvColumn("Amount")] public decimal Amount { get; set; }
    }

    [CsvParser]
    public partial class EndToEndParser
    {
        public partial IEnumerable<OptPair> ParseOptPairs(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<Aged> ParseAged(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<Money> ParseMoney(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<SemiMoney> ParseSemiMoney(TextReader reader, CsvOptions? options = null);
        public partial IAsyncEnumerable<Aged> ParseAgedAsync(TextReader reader, CsvOptions? options = null, CancellationToken ct = default);
        public partial IAsyncEnumerable<Pair> ParsePairsAsync(TextReader reader, CsvOptions? options = null, CancellationToken ct = default);
        public partial IEnumerable<MutableModel> ParseMutable(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<Other.MutableModel> ParseOtherMutable(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<Person> ParsePeople(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<CtorModel> ParseCtor(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<AllBuiltIns> ParseAll(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<Nullables> ParseNullables(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<Converted> ParseConverted(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<ConvertedNullable> ParseConvertedNullable(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<Semi> ParseSemi(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<Pair> ParsePairs(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<Pair> ParsePairsDefault(TextReader reader);
        public partial IEnumerable<Named> ParseNamed(TextReader reader, CsvOptions? options = null);
        public partial IEnumerable<KeywordModel> ParseKeyword(TextReader reader, CsvOptions? options = null);
    }
}

namespace CsvMole.Tests.EndToEnd.Other
{
    // Same short name as EndToEnd.MutableModel, in the same parser class: generated helpers must not clash.
    public sealed class MutableModel
    {
        [CsvOrder(0)] public int Value { get; set; }
    }
}
