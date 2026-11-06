// Layout mirrors FloatsTestData from Sep's comparison benchmarks
// (https://github.com/nietras/Sep, src/Sep.ComparisonBenchmarks/FloatsTestData.cs), MIT License, Copyright (c) nietras.
using System.Globalization;
using System.Text;

namespace CsvMole.Benchmarks.Data;

static class FloatsData
{
    /// <summary>
    /// ';'-separated, a header of GT_Feature0..n-1 then RE_Feature0..n-1, then <paramref name="rows"/> rows of
    /// Random(42).NextSingle() values formatted with the invariant culture.
    /// </summary>
    public static string Generate(int rows, int featureCount)
    {
        var random = new Random(42);
        var sb = new StringBuilder(rows * (featureCount * 2 * 11 + 2));
        for (var i = 0; i < featureCount; i++) sb.Append(i == 0 ? "" : ";").Append("GT_Feature").Append(i);
        for (var i = 0; i < featureCount; i++) sb.Append(";RE_Feature").Append(i);
        sb.AppendLine();
        for (var r = 0; r < rows; r++)
        {
            for (var i = 0; i < 2 * featureCount; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(random.NextSingle().ToString(CultureInfo.InvariantCulture));
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
