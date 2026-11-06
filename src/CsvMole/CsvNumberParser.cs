using System.ComponentModel;
using System.Globalization;

namespace CsvMole;

/// <summary>
/// Float and double parsing used by generated parsers. Gives the same results as <c>float.TryParse(span, provider)</c>
/// and <c>double.TryParse(span, provider)</c>, with an exact fast path for plain decimals such as <c>-12.345e-6</c>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CsvNumberParser
{
    // 10^0..10^22 are exact doubles.
    private static readonly double[] Pow10 =
    [
        1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11,
        1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22,
    ];

    public static bool TryParseDouble(ReadOnlySpan<char> s, IFormatProvider? provider, out double result) =>
        TryParseFast(s, provider, out result) || double.TryParse(s, provider, out result);

    public static bool TryParseSingle(ReadOnlySpan<char> s, IFormatProvider? provider, out float result)
    {
        // Rounding the correctly rounded double to float again is only wrong when the double lands exactly halfway
        // between two floats (the low 29 of its 52 mantissa bits are 1000...0); leave those to the BCL.
        if (TryParseFast(s, provider, out var d) && (BitConverter.DoubleToUInt64Bits(d) & 0x1FFF_FFFF) != 0x1000_0000)
        {
            result = (float)d;
            return true;
        }
        return float.TryParse(s, provider, out result);
    }

    /// <summary>
    /// Clinger's fast path: <c>[-]digits[.digits][(e|E)[+|-]digits]</c> with at most 2^53 as the mantissa and a power of
    /// ten within ±22 is one exact division or multiplication, so correctly rounded. Anything else returns false.
    /// </summary>
    private static bool TryParseFast(ReadOnlySpan<char> s, IFormatProvider? provider, out double result)
    {
        result = 0;
        var nfi = NumberFormatInfo.GetInstance(provider);
        if (nfi.NegativeSign != "-" || nfi.NumberDecimalSeparator.Length != 1) return false;
        var separator = nfi.NumberDecimalSeparator[0];

        var i = 0;
        var negative = s.Length > 0 && s[0] == '-';
        if (negative) i++;

        ulong mantissa = 0;
        var significant = 0;
        var digits = 0;
        var fraction = 0;
        var seenSeparator = false;
        for (; i < s.Length; i++)
        {
            var c = s[i];
            var digit = (uint)(c - '0');
            if (digit <= 9)
            {
                digits++;
                if (seenSeparator) fraction++;
                if (mantissa == 0 && digit == 0) continue; // leading zeros are not significant
                if (++significant > 19) return false;
                mantissa = mantissa * 10 + digit;
            }
            else if (c == separator && !seenSeparator)
            {
                seenSeparator = true;
            }
            else break;
        }
        if (digits == 0) return false;

        var exponent = 0;
        if (i < s.Length)
        {
            if (s[i] is not ('e' or 'E')) return false;
            i++;
            var negativeExponent = false;
            if (i < s.Length && s[i] == '-') { negativeExponent = true; i++; }
            else if (i < s.Length && s[i] == '+' && nfi.PositiveSign == "+") i++;
            var start = i;
            for (; i < s.Length && (uint)(s[i] - '0') <= 9; i++)
            {
                if (i - start >= 4) return false;
                exponent = exponent * 10 + (s[i] - '0');
            }
            if (i == start || i != s.Length) return false;
            if (negativeExponent) exponent = -exponent;
        }

        if (mantissa > 1UL << 53) return false;
        var power = exponent - fraction;
        if (power is < -22 or > 22) return false;

        double value = mantissa;
        value = power < 0 ? value / Pow10[-power] : value * Pow10[power];
        result = negative ? -value : value;
        return true;
    }
}
