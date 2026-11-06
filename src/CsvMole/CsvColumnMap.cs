using System.ComponentModel;

namespace CsvMole;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CsvColumnMap(int[] indexes, string?[] headerNames)
{
    public int[] Indexes { get; } = indexes;
    public string?[] HeaderNames { get; } = headerNames;
}
