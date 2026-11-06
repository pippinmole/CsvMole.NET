using CsvMole.Example.Models;

namespace CsvMole.Example;

[CsvParser]
public partial class OrderParser
{
    public partial IEnumerable<Order> Parse(TextReader reader, CsvOptions? options = null);

    public partial IAsyncEnumerable<Order> ParseAsync(TextReader reader, CsvOptions? options = null,
        CancellationToken cancellationToken = default);
}
