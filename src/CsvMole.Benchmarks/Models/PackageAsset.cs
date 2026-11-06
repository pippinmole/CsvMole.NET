// The 25 columns of NCsvPerf's PackageAsset (https://github.com/joelverhagen/NCsvPerf,
// NCsvPerf/CsvReadable/Benchmarks/PackageAsset.cs), MIT License, Copyright (c) Joel Verhagen.
using System.Globalization;

namespace CsvMole.Benchmarks.Models;

// A record so the [GlobalSetup] sanity check gets field-for-field equality for free.
public sealed record PackageAsset
{
    [CsvOrder(0)] public Guid? ScanId { get; init; }
    [CsvOrder(1), CsvConverter(typeof(NullableIsoDateTimeOffset))] public DateTimeOffset? ScanTimestamp { get; init; }
    [CsvOrder(2)] public string Id { get; init; } = "";
    [CsvOrder(3)] public string Version { get; init; } = "";
    [CsvOrder(4), CsvConverter(typeof(IsoDateTimeOffset))] public DateTimeOffset Created { get; init; }
    [CsvOrder(5)] public string ResultType { get; init; } = "";

    [CsvOrder(6)] public string PatternSet { get; init; } = "";
    [CsvOrder(7)] public string PropertyAnyValue { get; init; } = "";
    [CsvOrder(8)] public string PropertyCodeLanguage { get; init; } = "";
    [CsvOrder(9)] public string PropertyTargetFrameworkMoniker { get; init; } = "";
    [CsvOrder(10)] public string PropertyLocale { get; init; } = "";
    [CsvOrder(11)] public string PropertyManagedAssembly { get; init; } = "";
    [CsvOrder(12)] public string PropertyMSBuild { get; init; } = "";
    [CsvOrder(13)] public string PropertyRuntimeIdentifier { get; init; } = "";
    [CsvOrder(14)] public string PropertySatelliteAssembly { get; init; } = "";

    [CsvOrder(15)] public string Path { get; init; } = "";
    [CsvOrder(16)] public string FileName { get; init; } = "";
    [CsvOrder(17)] public string FileExtension { get; init; } = "";
    [CsvOrder(18)] public string TopLevelFolder { get; init; } = "";

    [CsvOrder(19)] public string RoundTripTargetFrameworkMoniker { get; init; } = "";
    [CsvOrder(20)] public string FrameworkName { get; init; } = "";
    [CsvOrder(21)] public string FrameworkVersion { get; init; } = "";
    [CsvOrder(22)] public string FrameworkProfile { get; init; } = "";
    [CsvOrder(23)] public string PlatformName { get; init; } = "";
    [CsvOrder(24)] public string PlatformVersion { get; init; } = "";

    // The same parse helpers NCsvPerf and Sep use, shared by every library in the benchmark.
    public static Guid? ParseNullableGuid(ReadOnlySpan<char> s) => s.Length > 0 ? Guid.Parse(s) : null;

    public static DateTimeOffset? ParseNullableDateTimeOffset(ReadOnlySpan<char> s) =>
        s.Length > 0 ? ParseDateTimeOffset(s) : null;

    public static DateTimeOffset ParseDateTimeOffset(ReadOnlySpan<char> s) =>
        DateTimeOffset.ParseExact(s, "O", CultureInfo.InvariantCulture);
}

public sealed class IsoDateTimeOffset : CsvConverter<DateTimeOffset>
{
    public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out DateTimeOffset result) =>
        DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
}

public sealed class NullableIsoDateTimeOffset : CsvConverter<DateTimeOffset?>
{
    public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out DateTimeOffset? result)
    {
        result = null;
        if (value.IsEmpty) return true;
        if (!DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return false;
        result = d;
        return true;
    }
}
