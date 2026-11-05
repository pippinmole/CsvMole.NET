namespace CsvMole;

public sealed class CsvParseException(CsvError error) : Exception(Format(error))
{
    public CsvError Error { get; } = error;

    private static string Format(CsvError e) =>
        $"CSV error ({e.Reason}) at line {e.LineNumber}, column {e.ColumnIndex}, " +
        $"header '{e.HeaderName}', member '{e.MemberName}', value '{e.RawValue}'.";
}
