using System.Collections.Immutable;
using CsvMole.SourceGenerators.Diagnostics;
using CsvMole.SourceGenerators.External;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CsvMole.SourceGenerators.Models;

internal sealed record DiagnosticInfo(string Id, LocationInfo? Location, EquatableArray<string> Args)
{
    public static DiagnosticInfo Create(string id, Location? location, params string[] args) =>
        new(id, LocationInfo.From(location), ImmutableArray.Create(args).AsEquatableArray());

    public Diagnostic ToDiagnostic() =>
        Diagnostic.Create(Descriptors.Get(Id), Location?.ToLocation(), Args.ToArray<object?>());
}

internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    public static LocationInfo? From(Location? location) =>
        location?.SourceTree is null
            ? null
            : new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);

    public Location ToLocation() => Microsoft.CodeAnalysis.Location.Create(FilePath, Span, LineSpan);
}
