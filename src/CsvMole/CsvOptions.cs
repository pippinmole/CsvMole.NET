using System.Globalization;

namespace CsvMole;

public sealed class CsvOptions
{
    public static CsvOptions Default { get; } = new();
    public bool HasHeader { get; init; } = true;
    public char? Delimiter { get; init; }
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;
    public bool SkipBlankLines { get; init; } = true;
    public bool TrimFields { get; init; } = false;
    public Action<CsvError>? OnError { get; init; }
    public int MaxRecordLength { get; init; } = 1024 * 1024;
}
