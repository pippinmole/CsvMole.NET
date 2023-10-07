namespace CsvMole.Abstractions.Options;

public readonly struct CsvOptions
{
    /// <summary>
    /// Indicates if the CSV file has a header row.
    /// </summary>
    public bool HasHeader { get; init; }
}