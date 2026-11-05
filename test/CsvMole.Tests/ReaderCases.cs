namespace CsvMole.Tests;

/// <summary>Shared (input, options, expected rows) cases for tests that compare reader entry points.</summary>
internal static class ReaderCases
{
    private static readonly CsvOptions Trim = new() { TrimFields = true };
    private static readonly CsvOptions Keep = new() { SkipBlankLines = false };
    private static readonly CsvOptions Max10 = new() { MaxRecordLength = 10 };

    private static string[][] R(params string[][] rows) => rows;
    private static string[] F(params string[] f) => f;

    public static IEnumerable<TestCaseData> All()
    {
        yield return Case("a,b\nc,d", null, R(F("a", "b"), F("c", "d")));
        yield return Case("a\r\nb\rc\nd", null, R(F("a"), F("b"), F("c"), F("d")));
        yield return Case("a\n", null, R(F("a")));
        yield return Case("", null, R());
        yield return Case("a,,c,", null, R(F("a", "", "c", "")));
        yield return Case("a\n\n\nb", null, R(F("a"), F("b")));
        yield return Case("a\n\nb", Keep, R(F("a"), F(""), F("b")));
        yield return Case("abc\r\r\ndef", Keep, R(F("abc"), F(""), F("def")));
        yield return Case("abcdefghijklmnopqrstuvwxyz,0123456789abcdefghij\r\nx,y", null,
            R(F("abcdefghijklmnopqrstuvwxyz", "0123456789abcdefghij"), F("x", "y")));
        yield return Case("a\n  \nb", Trim, R(F("a"), F(""), F("b")));
        yield return Case("abcdefghij\r\nx", Max10, R(F("abcdefghij"), F("x")));
        yield return Case(" a , b ", Trim, R(F("a", "b")));
        yield return Case("\"a,b\",c", null, R(F("a,b", "c")));
        yield return Case("\"say \"\"hi\"\"\"", null, R(F("say \"hi\"")));
        yield return Case("\"x\ny\",1\nz,2", null, R(F("x\ny", "1"), F("z", "2")));
        yield return Case("\"\",a", null, R(F("", "a")));
        yield return Case("ab\"c,d", null, R(F("ab\"c", "d")));
        yield return Case("a,  \"b,c\"", Trim, R(F("a", "b,c")));
        yield return Case("a,  \"b,c\"", null, R(F("a", "  \"b", "c\"")));
        yield return Case("\" a \"", Trim, R(F(" a ")));
        yield return Case("\"\"\"\",x,\"a\"\"b\"\r\n\"c\"\"\",\"d\"", null, R(F("\"", "x", "a\"b"), F("c\"", "d")));
        yield return Case("\"a\r\nb\rc\",1\r\nz", null, R(F("a\r\nb\rc", "1"), F("z")));
        yield return Case(" \"a\"  ,b", Trim, R(F("a", "b")));
        yield return Case("\"a,b\"x,c", null, R(F("\"a,b\"x", "c")));
        yield return Case("\"ab\ncdef\"\nx", Max10, R(F("ab\ncdef"), F("x")));
    }

    private static TestCaseData Case(string input, CsvOptions? options, string[][] expected) =>
        new TestCaseData(input, options ?? new CsvOptions(), expected).SetArgDisplayNames(input.Replace("\r", "\r").Replace("\n", "\n"));
}
