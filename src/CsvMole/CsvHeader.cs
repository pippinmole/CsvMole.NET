namespace CsvMole;

public static class CsvHeader
{
    /// <summary>
    /// Maps each member to a column index once per file. names[i] is the [CsvColumn] name or null, orders[i] the
    /// [CsvOrder] index or -1. header is the reader positioned on the header row, or null when there is none.
    /// </summary>
    public static CsvColumnMap Resolve(CsvReader? header, string?[] names, int[] orders, string[] memberNames)
    {
        var indexes = new int[names.Length];
        var headerNames = new string?[names.Length];

        string[] cells = [];
        if (header is not null)
        {
            cells = new string[header.FieldCount];
            for (var c = 0; c < cells.Length; c++)
            {
                var span = header[c];
                if (c == 0 && span.Length > 0 && span[0] == '﻿') span = span[1..];
                cells[c] = span.Trim().ToString();
            }
        }

        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            if (name is null)
            {
                indexes[i] = orders[i];
                headerNames[i] = header is not null && orders[i] >= 0 && orders[i] < cells.Length ? cells[orders[i]] : null;
                continue;
            }

            if (header is null) throw Error(header, CsvErrorReason.UnresolvableColumn, name, memberNames[i]);

            var found = -1;
            for (var c = 0; c < cells.Length; c++)
            {
                if (!string.Equals(cells[c], name.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                if (found >= 0) throw Error(header, CsvErrorReason.DuplicateHeaderColumn, name, memberNames[i]);
                found = c;
            }
            if (found < 0) throw Error(header, CsvErrorReason.MissingHeaderColumn, name, memberNames[i]);
            indexes[i] = found;
            headerNames[i] = cells[found];
        }

        return new CsvColumnMap(indexes, headerNames);
    }

    private static CsvParseException Error(CsvReader? header, CsvErrorReason reason, string name, string member) =>
        new(new CsvError(header?.LineNumber ?? 0, -1, name, member, null, reason));
}
