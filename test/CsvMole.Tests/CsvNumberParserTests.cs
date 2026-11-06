using System.Globalization;

namespace CsvMole.Tests;

// CsvNumberParser must agree bit-for-bit with float/double.TryParse(span, provider), fast path or not.
public class CsvNumberParserTests
{
    private static readonly string[] Cultures = ["", "de-DE", "sv-SE", "fr-FR"];

    private static void AssertSame(string s, CultureInfo culture)
    {
        var okF = CsvNumberParser.TryParseSingle(s, culture, out var f);
        var expectF = float.TryParse(s, culture, out var ef);
        Assert.That((okF, BitConverter.SingleToInt32Bits(f)), Is.EqualTo((expectF, BitConverter.SingleToInt32Bits(ef))), $"float '{s}' {culture.Name}");

        var okD = CsvNumberParser.TryParseDouble(s, culture, out var d);
        var expectD = double.TryParse(s, culture, out var ed);
        Assert.That((okD, BitConverter.DoubleToInt64Bits(d)), Is.EqualTo((expectD, BitConverter.DoubleToInt64Bits(ed))), $"double '{s}' {culture.Name}");
    }

    [Test]
    public void EdgeCases([ValueSource(nameof(Cultures))] string name)
    {
        string[] inputs =
        [
            "", "-", ".", ",", "-.", "e5", "1e", "1e+", "1e-", "0", "-0", "00", "5.", ".5", "-.5", "1,5", "1.5", "1.234,5", "1,234.5",
            "1e5", "1E5", "1e+5", "1e-5", "1e22", "1e23", "1e-22", "1e-23", "1.5e300", "1e-400", " 1", "1 ", "+1", "--1", "1-",
            "9007199254740992", "9007199254740993", "18446744073709551615", "12345678901234567890", "0.000000000000000000001",
            "1.00000000000000000000", "0.1", "0.2", "0.3", "3.4028235E+38", "3.4028236E+38", "NaN", "Infinity", "-Infinity",
            "1e00001", "1.2.3", "1e5.5", "16777217", "33554435", "1.00000005960464477539", "0.52499354", "1−5", "−1.5",
        ];
        foreach (var s in inputs) AssertSame(s, CultureInfo.GetCultureInfo(name));
    }

    [Test]
    public void RandomValues_MatchBcl([Values(1, 2, 3)] int seed)
    {
        var random = new Random(seed);
        string[] formats = ["R", "G9", "G17", "F2", "F7", "E3", "E10", "0.###############"];
        var cultures = Cultures.Select(CultureInfo.GetCultureInfo).ToArray();
        for (var i = 0; i < 20_000; i++)
        {
            var value = random.Next(4) switch
            {
                0 => random.NextSingle(),
                1 => (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-30, 30)),
                2 => random.Next(-100_000, 100_000) / 1000.0,
                _ => BitConverter.Int32BitsToSingle(random.Next()),
            };
            var culture = cultures[i % cultures.Length];
            AssertSame(value.ToString(formats[random.Next(formats.Length)], culture), culture);
        }
    }

    // Near a float midpoint, rounding the double to float must not round twice (short forms hit the fast path).
    [Test]
    public void FloatMidpoints_RoundOnce([Values(0x3F000000, 0x4B800000, 0x2E000000, 0x5E000000)] int baseBits)
    {
        for (var bits = baseBits; bits < baseBits + 3000; bits++)
        {
            var mid = ((double)BitConverter.Int32BitsToSingle(bits) + BitConverter.Int32BitsToSingle(bits + 1)) / 2;
            foreach (var format in new[] { "R", "G15", "G16", "E14" })
                AssertSame(mid.ToString(format, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }
    }
}
