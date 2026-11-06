// Mirrors PackageAssetsTestData.LineCache from Sep's comparison benchmarks
// (https://github.com/nietras/Sep, src/Sep.ComparisonBenchmarks/PackageAssetsTestData.cs), MIT License,
// Copyright (c) nietras. Data: PackageAssets.csv from NCsvPerf (https://github.com/joelverhagen/NCsvPerf), MIT License.
using System.Runtime.CompilerServices;
using System.Text;

namespace CsvMole.Benchmarks.Data;

static class PackageAssetsData
{
    const string Url = "https://raw.githubusercontent.com/joelverhagen/NCsvPerf/main/NCsvPerf/TestData/PackageAssets.csv";

    /// <summary>
    /// The first <paramref name="rows"/> lines (no header), cycling the source file as Sep does. Columns 0, 1 and 4
    /// (parsed as Guid/DateTimeOffset) are never quoted or padded.
    /// </summary>
    public static string Get(int rows, bool quoteSomeColumns = false, bool spacesAndQuotes = false)
    {
        var lines = File.ReadAllLines(EnsureFile());
        if (quoteSomeColumns || spacesAndQuotes)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < lines.Length; i++)
            {
                sb.Clear();
                AddQuotesAndMaybeSpaces(lines[i], sb, quoteSomeColumns || spacesAndQuotes, spacesAndQuotes);
                lines[i] = sb.ToString();
            }
        }

        var text = new StringBuilder(rows * 320);
        if (rows > 0) text.AppendLine(lines[0]);
        for (var line = 1; line < rows; line++)
            text.AppendLine(lines[(line - 1) % (lines.Length - 1) + 1]);
        return text.ToString();
    }

    static void AddQuotesAndMaybeSpaces(ReadOnlySpan<char> span, StringBuilder sb, bool quotes, bool spaces)
    {
        var col = 0;
        var start = 0;
        while (start <= span.Length)
        {
            var remain = span[start..];
            var found = remain.IndexOf(',');
            found = found < 0 ? span.Length - start : found;
            var colSpan = remain[..found];
            if (col > 0) sb.Append(',');
            if (col is 0 or 1 or 4)
            {
                sb.Append(colSpan);
            }
            else
            {
                if (spaces) sb.Append(' ');
                if (quotes) sb.Append('"');
                if (spaces) sb.Append(' ');
                sb.Append(colSpan);
                if (spaces) sb.Append(' ');
                if (quotes) sb.Append('"');
                if (spaces) sb.Append(' ');
            }
            start += found + 1;
            ++col;
        }
    }

    static string EnsureFile([CallerFilePath] string thisFile = "")
    {
        // shortcut: locates data/ via this source file's compile-time path, so the benchmarks must run on the machine that built them.
        var dir = Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "data");
        var path = Path.GetFullPath(Path.Combine(dir, "PackageAssets.csv"));
        if (File.Exists(path)) return path;

        Directory.CreateDirectory(dir);
        using var client = new HttpClient();
        var bytes = client.GetByteArrayAsync(Url).GetAwaiter().GetResult();
        File.WriteAllBytes(path, bytes);
        return path;
    }
}
