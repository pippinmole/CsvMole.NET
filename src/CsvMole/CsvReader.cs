using System.Buffers;
using System.Runtime.CompilerServices;

namespace CsvMole;

/// <summary>
/// Low-level CSV tokenizer used by generated parsers. Reads one record at a time and exposes its fields as spans
/// that are valid only until the next <see cref="Read"/>.
/// </summary>
public sealed class CsvReader : IDisposable
{
    private const int DefaultBufferSize = 16 * 1024;

    private readonly TextReader _reader;
    private readonly char _delimiter;
    private readonly SearchValues<char> _special;
    private readonly bool _trim;
    private readonly bool _skipBlank;
    private readonly int _maxRecordLength;

    private char[] _buffer;
    private int _start;  // first unconsumed char
    private int _end;    // end of valid data
    private bool _eof;
    private long _nextLine = 1;

    // Field i is _buffer[_bounds[2i] .. _bounds[2i+1]).
    private int[] _bounds;
    private int _fieldCount;
    private bool _disposed;

    public CsvReader(TextReader reader, CsvOptions options, char delimiter)
        : this(reader, options, delimiter, DefaultBufferSize) { }

    internal CsvReader(TextReader reader, CsvOptions options, char delimiter, int initialBufferSize)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(options);
        if (delimiter is '"' or '\r' or '\n')
            throw new ArgumentException("Delimiter cannot be a quote or a line break.", nameof(delimiter));
        ArgumentOutOfRangeException.ThrowIfLessThan(initialBufferSize, 1);

        _reader = reader;
        Options = options;
        _delimiter = delimiter;
        _special = SearchValues.Create([delimiter, '"', '\r', '\n']);
        _trim = options.TrimFields;
        _skipBlank = options.SkipBlankLines;
        _maxRecordLength = options.MaxRecordLength;
        _buffer = ArrayPool<char>.Shared.Rent(initialBufferSize);
        _bounds = ArrayPool<int>.Shared.Rent(64);
    }

    public CsvOptions Options { get; }

    internal int BufferLength => _buffer.Length; // tests: proves refill shifts instead of growing

    public int FieldCount => _fieldCount;

    /// <summary>1-based physical line where the current record starts.</summary>
    public long LineNumber { get; private set; }

    public ReadOnlySpan<char> this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_fieldCount)
                ThrowIndex(index);
            var s = _bounds[2 * index];
            return _buffer.AsSpan(s, _bounds[2 * index + 1] - s);
        }
    }

    public bool Read()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        while (true)
        {
            switch (TryReadRecord())
            {
                case ReadResult.Record: return true;
                case ReadResult.End: return false;
                default: Refill(); break;
            }
        }
    }

    private enum ReadResult { Record, NeedMore, End }

    /// <summary>Finds the next record in the buffered data. Shared by sync and (future) async Read.</summary>
    private ReadResult TryReadRecord()
    {
        while (true)
        {
            var recordStart = _start;
            var fieldStart = recordStart;
            var pos = recordStart;
            _fieldCount = 0;

            while (true)
            {
                var idx = _buffer.AsSpan(pos, _end - pos).IndexOfAny(_special);
                if (idx < 0)
                {
                    CheckLength(recordStart, _end);
                    if (!_eof) return ReadResult.NeedMore;
                    if (recordStart == _end && _fieldCount == 0) return ReadResult.End;
                    AddField(fieldStart, _end);
                    return Complete(_end);
                }

                var p = pos + idx;
                var c = _buffer[p];
                if (c == _delimiter)
                {
                    AddField(fieldStart, p);
                    fieldStart = pos = p + 1;
                }
                else if (c == '"')
                {
                    pos = p + 1; // shortcut: quotes are literal until Task 4 adds RFC 4180 quoting
                }
                else
                {
                    CheckLength(recordStart, p);
                    var terminator = 1;
                    if (c == '\r')
                    {
                        // A trailing \r may be the first half of a CRLF split across refills.
                        if (p + 1 == _end && !_eof) return ReadResult.NeedMore;
                        if (p + 1 < _end && _buffer[p + 1] == '\n') terminator = 2;
                    }

                    if (_skipBlank && p == recordStart)
                    {
                        _start = p + terminator;
                        _nextLine++;
                        break; // blank line: look for the next record
                    }

                    AddField(fieldStart, p);
                    return Complete(p + terminator);
                }
            }
        }
    }

    private ReadResult Complete(int next)
    {
        LineNumber = _nextLine;
        _nextLine++;
        _start = next;
        return ReadResult.Record;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddField(int start, int end)
    {
        if (_trim)
        {
            while (start < end && char.IsWhiteSpace(_buffer[start])) start++;
            while (end > start && char.IsWhiteSpace(_buffer[end - 1])) end--;
        }

        var i = 2 * _fieldCount;
        if (i + 1 >= _bounds.Length) GrowBounds();
        _bounds[i] = start;
        _bounds[i + 1] = end;
        _fieldCount++;
    }

    private void GrowBounds()
    {
        var bigger = ArrayPool<int>.Shared.Rent(_bounds.Length * 2);
        _bounds.AsSpan(0, 2 * _fieldCount).CopyTo(bigger);
        ArrayPool<int>.Shared.Return(_bounds);
        _bounds = bigger;
    }

    private void CheckLength(int recordStart, int contentEnd)
    {
        if (contentEnd - recordStart <= _maxRecordLength) return;
        var raw = _buffer.AsSpan(recordStart, contentEnd - recordStart);
        throw new CsvParseException(new CsvError(
            _nextLine, _fieldCount, null, null, CsvErrors.Truncate(raw), CsvErrorReason.RecordTooLong));
    }

    /// <summary>Moves the unconsumed tail to the front, grows if a single record fills the buffer, and reads more.</summary>
    private void Refill()
    {
        PrepareRefill();
        var read = _reader.Read(_buffer, _end, _buffer.Length - _end);
        if (read == 0) _eof = true;
        _end += read;
    }

    private void PrepareRefill()
    {
        if (_start > 0)
        {
            _buffer.AsSpan(_start, _end - _start).CopyTo(_buffer);
            _end -= _start;
            _start = 0;
        }

        if (_end == _buffer.Length)
        {
            var bigger = ArrayPool<char>.Shared.Rent(_buffer.Length * 2);
            _buffer.AsSpan(0, _end).CopyTo(bigger);
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = bigger;
        }
    }

    private static void ThrowIndex(int index) =>
        throw new ArgumentOutOfRangeException(nameof(index), index, "Field index is out of range.");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _fieldCount = 0;
        ArrayPool<char>.Shared.Return(_buffer);
        ArrayPool<int>.Shared.Return(_bounds);
        _buffer = [];
        _bounds = [];
    }
}
