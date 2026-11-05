namespace CsvMole;

public enum CsvErrorReason
{
    ParseFailed,
    TooFewColumns,
    UnterminatedQuote,
    RecordTooLong,
    MissingHeaderColumn,
    DuplicateHeaderColumn,
    UnresolvableColumn,
}

public sealed record CsvError(
    long LineNumber, int ColumnIndex, string? HeaderName, string? MemberName, string? RawValue, CsvErrorReason Reason);
