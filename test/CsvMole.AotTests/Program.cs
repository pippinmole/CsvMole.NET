using System.Globalization;
using CsvMole;

const string csv = "Id,Name,Score\n1,Ann,x10\n2,Bob,x20\n3,Cy,x30\n";

var parser = new AotParser();
var sync = parser.Parse(new StringReader(csv)).ToList();
var async = new List<Row>();
await foreach (var r in parser.ParseAsync(new StringReader(csv))) async.Add(r);

static bool Ok(List<Row> rows) =>
    rows.Count == 3 && rows[0] == new Row(1, "Ann", 10) && rows[1] == new Row(2, "Bob", 20) && rows[2] == new Row(3, "Cy", 30);

return Ok(sync) && Ok(async) ? 0 : 1;

public sealed class XConverter : CsvConverter<int>
{
    public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out int result)
    {
        result = 0;
        return value.StartsWith("x") && int.TryParse(value[1..], culture, out result);
    }
}

public sealed record Row(
    [property: CsvColumn("Id")] int Id,
    [property: CsvColumn("Name")] string Name,
    [property: CsvColumn("Score"), CsvConverter(typeof(XConverter))] int Score);

[CsvParser]
public partial class AotParser
{
    public partial IEnumerable<Row> Parse(TextReader reader, CsvOptions? options = null);
    public partial IAsyncEnumerable<Row> ParseAsync(TextReader reader, CsvOptions? options = null, CancellationToken ct = default);
}
