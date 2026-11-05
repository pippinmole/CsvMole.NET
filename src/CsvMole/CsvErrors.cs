namespace CsvMole;

public static class CsvErrors
{
    private const int MaxRaw = 200;

    public static void Report(CsvOptions options, CsvError error)
    {
        var recoverable = error.Reason is CsvErrorReason.ParseFailed or CsvErrorReason.TooFewColumns;
        if (options.OnError is null || !recoverable)
            throw new CsvParseException(error);
        options.OnError(error);
    }

    public static string Truncate(ReadOnlySpan<char> raw) =>
        raw.Length <= MaxRaw ? raw.ToString() : string.Concat(raw[..MaxRaw], "…");
}
