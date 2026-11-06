using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

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

    // Field i is _buffer[_bounds[2i] .. _bounds[2i+1]), or _scratch[~_bounds[2i] .. _bounds[2i+1]) when _bounds[2i] < 0.
    private int[] _bounds;
    private int _fieldCount;
    private bool _disposed;

    // Unescaped ("" -> ") quoted fields of the current record.
    private char[] _scratch = [];
    private int _scratchLength;

    // Scan state of the in-progress record (absolute buffer indices, shifted by PrepareRefill), kept across NeedMore
    // so a record trickling in is scanned once instead of rescanned from its start on every refill.
    private bool _inRecord;
    private int _pos;
    private int _fieldStart;
    private QuoteState _quote;
    private int _quoteStart;   // first char after the opening quote
    private int _quoteEnd;     // the closing quote
    private bool _hasEscape;
    private int _quotedLines;  // line breaks inside quotes, so LineNumber stays physical

    private enum QuoteState { None, Inside, Closed }

    // Bitmask of the specials (delimiter, quote, CR, LF) in the 32 chars at _maskPos, so the buffer is classified once per
    // 32-char block instead of once per field. Bits below the scan point are ignored. Reset by PrepareRefill.
    private const int NoMask = -64;
    private int _maskPos = NoMask;
    private uint _mask;

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
            var e = _bounds[2 * index + 1];
            return s >= 0 ? _buffer.AsSpan(s, e - s) : _scratch.AsSpan(~s, e - ~s);
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

    /// <summary>
    /// Async <see cref="Read"/>: completes synchronously when a full record is already buffered. Cancellation is only
    /// observed when a refill is needed. Parse errors surface through the returned task.
    /// </summary>
    public ValueTask<bool> ReadAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            return TryReadRecord() switch
            {
                ReadResult.Record => new ValueTask<bool>(true),
                ReadResult.End => new ValueTask<bool>(false),
                _ => ReadAsyncSlow(cancellationToken),
            };
        }
        catch (CsvParseException e)
        {
            return ValueTask.FromException<bool>(e);
        }
    }

    private async ValueTask<bool> ReadAsyncSlow(CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            PrepareRefill();
            var read = await _reader.ReadAsync(_buffer.AsMemory(_end), ct).ConfigureAwait(false);
            if (read == 0) _eof = true;
            _end += read;
            switch (TryReadRecord())
            {
                case ReadResult.Record: return true;
                case ReadResult.End: return false;
            }
        }
    }

    private enum ReadResult { Record, NeedMore, End }

    /// <summary>Finds the next record in the buffered data. Shared by Read and ReadAsync.</summary>
    private ReadResult TryReadRecord()
    {
        while (true)
        {
            if (!_inRecord)
            {
                _inRecord = true;
                _pos = _fieldStart = _start;
                _fieldCount = 0;
                _scratchLength = 0;
                _quotedLines = 0;
            }

            var recordStart = _start;
            while (true)
            {
                if (_quote == QuoteState.Inside)
                {
                    // Delimiters are literal inside quotes, so skip them.
                    var q = NextSpecial(_pos);
                    while (q >= 0 && _buffer[q] == _delimiter) q = NextSpecial(q + 1);
                    if (q < 0)
                    {
                        CheckLength(recordStart, _end);
                        _pos = _end;
                        if (!_eof) return ReadResult.NeedMore;
                        throw new CsvParseException(new CsvError(_nextLine, _fieldCount, null, null,
                            CsvErrors.Truncate(_buffer.AsSpan(recordStart, _end - recordStart)), CsvErrorReason.UnterminatedQuote));
                    }

                    var qc = _buffer[q];
                    if (qc != '"')
                    {
                        // Count \r, and \n not preceded by \r. The opening quote precedes q, so q - 1 is in the record.
                        if (qc == '\r' || _buffer[q - 1] != '\r') _quotedLines++;
                        _pos = q + 1;
                    }
                    else if (q + 1 == _end && !_eof)
                    {
                        _pos = q; // may be the first half of "" split across refills
                        CheckLength(recordStart, _end);
                        return ReadResult.NeedMore;
                    }
                    else if (q + 1 < _end && _buffer[q + 1] == '"')
                    {
                        _hasEscape = true;
                        _pos = q + 2;
                    }
                    else if (q + 1 < _end && _buffer[q + 1] == _delimiter && !_hasEscape)
                    {
                        // Fast path: closing quote right before the delimiter, no "" inside (as EndField would do).
                        _quote = QuoteState.None;
                        AddBounds(_quoteStart, q);
                        _fieldStart = _pos = q + 2;
                    }
                    else
                    {
                        _quote = QuoteState.Closed;
                        _quoteEnd = q;
                        _pos = q + 1;
                    }
                    continue;
                }

                var p = NextSpecial(_pos);
                if (p < 0)
                {
                    CheckLength(recordStart, _end);
                    _pos = _end;
                    if (!_eof) return ReadResult.NeedMore;
                    if (recordStart == _end && _fieldCount == 0)
                    {
                        _inRecord = false;
                        return ReadResult.End;
                    }
                    EndField(_end);
                    return Complete(_end);
                }

                var c = _buffer[p];
                if (c == _delimiter)
                {
                    if (_quote == QuoteState.None && !_trim) AddBounds(_fieldStart, p); // fast path: plain field
                    else EndField(p);
                    _fieldStart = _pos = p + 1;
                }
                else if (c == '"')
                {
                    if (_quote == QuoteState.None && IsOpeningQuote(p))
                    {
                        _quote = QuoteState.Inside;
                        _quoteStart = p + 1;
                    }
                    _pos = p + 1; // otherwise a literal quote mid-field
                }
                else
                {
                    CheckLength(recordStart, p);
                    var terminator = 1;
                    if (c == '\r')
                    {
                        // A trailing \r may be the first half of a CRLF split across refills.
                        if (p + 1 == _end && !_eof)
                        {
                            _pos = p;
                            return ReadResult.NeedMore;
                        }
                        if (p + 1 < _end && _buffer[p + 1] == '\n') terminator = 2;
                    }

                    if (_skipBlank && p == recordStart)
                    {
                        _start = p + terminator;
                        _nextLine++;
                        _inRecord = false;
                        break; // blank line: look for the next record
                    }

                    EndField(p);
                    return Complete(p + terminator);
                }
            }
        }
    }

    /// <summary>Absolute index of the first special char at or after <paramref name="pos"/> in the buffered data, or -1.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NextSpecial(int pos)
    {
        var offset = pos - _maskPos;
        if ((uint)offset < 32)
        {
            var m = _mask & (uint.MaxValue << offset);
            if (m != 0) return _maskPos + BitOperations.TrailingZeroCount(m);
            pos = _maskPos + 32;
        }
        return NextSpecialSlow(pos);
    }

    private int NextSpecialSlow(int pos)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            ref var data = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetArrayDataReference(_buffer));
            var delimiter = Vector256.Create((ushort)_delimiter);
            var quote = Vector256.Create((ushort)'"');
            var cr = Vector256.Create((ushort)'\r');
            var lf = Vector256.Create((ushort)'\n');
            for (; pos + 32 <= _end; pos += 32)
            {
                var a = Vector256.LoadUnsafe(ref data, (nuint)pos);
                var b = Vector256.LoadUnsafe(ref data, (nuint)pos + 16);
                var ma = Vector256.Equals(a, delimiter) | Vector256.Equals(a, quote) | Vector256.Equals(a, cr) | Vector256.Equals(a, lf);
                var mb = Vector256.Equals(b, delimiter) | Vector256.Equals(b, quote) | Vector256.Equals(b, cr) | Vector256.Equals(b, lf);
                var m = ma.ExtractMostSignificantBits() | (mb.ExtractMostSignificantBits() << 16);
                if (m != 0)
                {
                    _maskPos = pos;
                    _mask = m;
                    return pos + BitOperations.TrailingZeroCount(m);
                }
            }
        }

        // Fewer than 32 chars buffered past pos (or no 256-bit vectors).
        var idx = _buffer.AsSpan(pos, _end - pos).IndexOfAny(_special);
        return idx < 0 ? -1 : pos + idx;
    }

    // RFC 4180: a quote opens a quoted field only as its first char; with TrimFields, leading whitespace is skipped first.
    private bool IsOpeningQuote(int p) =>
        p == _fieldStart || (_trim && _buffer.AsSpan(_fieldStart, p - _fieldStart).IsWhiteSpace());

    private ReadResult Complete(int next)
    {
        LineNumber = _nextLine;
        _nextLine += 1 + _quotedLines;
        _start = next;
        _inRecord = false;
        return ReadResult.Record;
    }

    private void EndField(int end)
    {
        var quoted = _quote == QuoteState.Closed;
        var escaped = _hasEscape;
        _quote = QuoteState.None;
        _hasEscape = false;

        // Anything but (trimmed) whitespace after the closing quote: keep the whole field raw, quotes literal.
        if (!quoted || !IsBlankAfterQuote(end))
        {
            AddField(_fieldStart, end);
            return;
        }

        if (!escaped)
        {
            AddBounds(_quoteStart, _quoteEnd);
            return;
        }

        var raw = _buffer.AsSpan(_quoteStart, _quoteEnd - _quoteStart);
        if (_scratchLength + raw.Length > _scratch.Length) GrowScratch(_scratchLength + raw.Length);
        var from = _scratchLength;
        for (var i = 0; i < raw.Length; i++)
        {
            _scratch[_scratchLength++] = raw[i];
            if (raw[i] == '"') i++; // inside a closed quoted field every quote is doubled
        }
        AddBounds(~from, _scratchLength);
    }

    private bool IsBlankAfterQuote(int end)
    {
        var tail = _buffer.AsSpan(_quoteEnd + 1, end - _quoteEnd - 1);
        return tail.IsEmpty || (_trim && tail.IsWhiteSpace());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddField(int start, int end)
    {
        if (_trim)
        {
            while (start < end && char.IsWhiteSpace(_buffer[start])) start++;
            while (end > start && char.IsWhiteSpace(_buffer[end - 1])) end--;
        }
        AddBounds(start, end);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddBounds(int start, int end)
    {
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

    private void GrowScratch(int needed)
    {
        var bigger = ArrayPool<char>.Shared.Rent(Math.Max(needed, 2 * _scratch.Length));
        _scratch.AsSpan(0, _scratchLength).CopyTo(bigger);
        if (_scratch.Length > 0) ArrayPool<char>.Shared.Return(_scratch);
        _scratch = bigger;
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
        _maskPos = NoMask;
        if (_start > 0)
        {
            var shift = _start;
            _buffer.AsSpan(shift, _end - shift).CopyTo(_buffer);
            _end -= shift;
            _start = 0;

            // Only reached after NeedMore, so the scan state belongs to the in-progress record and moves with it.
            _pos -= shift;
            _fieldStart -= shift;
            _quoteStart -= shift;
            _quoteEnd -= shift;
            for (var i = 0; i < 2 * _fieldCount; i += 2)
            {
                if (_bounds[i] < 0) continue; // scratch field
                _bounds[i] -= shift;
                _bounds[i + 1] -= shift;
            }
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
        if (_scratch.Length > 0) ArrayPool<char>.Shared.Return(_scratch);
        _buffer = [];
        _bounds = [];
        _scratch = [];
    }
}
