namespace CsvMole.Benchmarks.Models;

// Sep's FloatsTestData layout with 20 features: GT_Feature0..19 then RE_Feature0..19, ';'-separated.
[CsvDelimiter(';')]
public sealed record FloatsRow
{
    [CsvOrder(0)] public float GT_Feature0 { get; init; }
    [CsvOrder(1)] public float GT_Feature1 { get; init; }
    [CsvOrder(2)] public float GT_Feature2 { get; init; }
    [CsvOrder(3)] public float GT_Feature3 { get; init; }
    [CsvOrder(4)] public float GT_Feature4 { get; init; }
    [CsvOrder(5)] public float GT_Feature5 { get; init; }
    [CsvOrder(6)] public float GT_Feature6 { get; init; }
    [CsvOrder(7)] public float GT_Feature7 { get; init; }
    [CsvOrder(8)] public float GT_Feature8 { get; init; }
    [CsvOrder(9)] public float GT_Feature9 { get; init; }
    [CsvOrder(10)] public float GT_Feature10 { get; init; }
    [CsvOrder(11)] public float GT_Feature11 { get; init; }
    [CsvOrder(12)] public float GT_Feature12 { get; init; }
    [CsvOrder(13)] public float GT_Feature13 { get; init; }
    [CsvOrder(14)] public float GT_Feature14 { get; init; }
    [CsvOrder(15)] public float GT_Feature15 { get; init; }
    [CsvOrder(16)] public float GT_Feature16 { get; init; }
    [CsvOrder(17)] public float GT_Feature17 { get; init; }
    [CsvOrder(18)] public float GT_Feature18 { get; init; }
    [CsvOrder(19)] public float GT_Feature19 { get; init; }
    [CsvOrder(20)] public float RE_Feature0 { get; init; }
    [CsvOrder(21)] public float RE_Feature1 { get; init; }
    [CsvOrder(22)] public float RE_Feature2 { get; init; }
    [CsvOrder(23)] public float RE_Feature3 { get; init; }
    [CsvOrder(24)] public float RE_Feature4 { get; init; }
    [CsvOrder(25)] public float RE_Feature5 { get; init; }
    [CsvOrder(26)] public float RE_Feature6 { get; init; }
    [CsvOrder(27)] public float RE_Feature7 { get; init; }
    [CsvOrder(28)] public float RE_Feature8 { get; init; }
    [CsvOrder(29)] public float RE_Feature9 { get; init; }
    [CsvOrder(30)] public float RE_Feature10 { get; init; }
    [CsvOrder(31)] public float RE_Feature11 { get; init; }
    [CsvOrder(32)] public float RE_Feature12 { get; init; }
    [CsvOrder(33)] public float RE_Feature13 { get; init; }
    [CsvOrder(34)] public float RE_Feature14 { get; init; }
    [CsvOrder(35)] public float RE_Feature15 { get; init; }
    [CsvOrder(36)] public float RE_Feature16 { get; init; }
    [CsvOrder(37)] public float RE_Feature17 { get; init; }
    [CsvOrder(38)] public float RE_Feature18 { get; init; }
    [CsvOrder(39)] public float RE_Feature19 { get; init; }
}
