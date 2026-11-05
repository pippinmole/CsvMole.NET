using Microsoft.CodeAnalysis;

namespace CsvMole.SourceGenerators.Tests;

public class ModelDiagnosticsTests
{
    private const string Prelude = """
        using System;
        using System.Collections.Generic;
        using System.Globalization;
        using System.IO;
        using CsvMole;

        [CsvParser]
        public partial class P { public partial IEnumerable<M> Parse(TextReader reader); }

        """;

    private static Diagnostic[] AssertDiagnostics(string model, params string[] expectedIds)
    {
        var (gen, comp, _) = GeneratorHarness.Run(Prelude + model);
        Assert.That(gen.Select(d => d.Id).Distinct().Order(), Is.EqualTo(expectedIds), string.Join("\n", gen));
        Assert.That(comp, Is.Empty, string.Join("\n", comp));
        Assert.That(gen.Where(d => d.Id != "CSVM010").Select(d => d.Location.GetLineSpan().Path), Is.All.EqualTo("Source.cs"));
        return gen.ToArray();
    }

    [Test]
    public void UnsupportedType_CSVM003() => AssertDiagnostics("""
        public class M { [CsvOrder(0)] public Stream? S { get; set; } }
        """, "CSVM003");

    [Test]
    public void DuplicateOrder_CSVM004() => AssertDiagnostics("""
        public class M { [CsvOrder(1)] public int A { get; set; } [CsvOrder(1)] public int B { get; set; } }
        """, "CSVM004");

    [Test]
    public void DuplicateColumnNameCaseInsensitive_CSVM004() => AssertDiagnostics("""
        public class M { [CsvColumn("Id")] public int A { get; set; } [CsvColumn("id")] public int B { get; set; } }
        """, "CSVM004");

    [Test]
    public void NegativeOrder_CSVM004() => AssertDiagnostics("""
        public class M { [CsvOrder(-1)] public int A { get; set; } }
        """, "CSVM004");

    [Test]
    public void GetOnlyMapped_CSVM005() => AssertDiagnostics("""
        public class M { [CsvOrder(0)] public int A { get; } }
        """, "CSVM005");

    [Test]
    public void RequiredUnmapped_CSVM006() => AssertDiagnostics("""
        public class M { [CsvOrder(0)] public int A { get; set; } public required string Name { get; set; } }
        """, "CSVM006");

    [Test]
    public void TwoCtorsUnmarked_CSVM007() => AssertDiagnostics("""
        public class M
        {
            public M() { }
            public M(int a) { A = a; }
            [CsvOrder(0)] public int A { get; set; }
        }
        """, "CSVM007");

    [Test]
    public void TwoCtorsOneMarked_NoDiagnostic() => AssertDiagnostics("""
        public class M
        {
            public M() { }
            [CsvConstructor] public M(int a) { A = a; }
            [CsvOrder(0)] public int A { get; }
        }
        """);

    [Test]
    public void ConverterWrongT_CSVM008() => AssertDiagnostics("""
        public sealed class C : CsvConverter<string>
        {
            public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out string result) { result = ""; return true; }
        }
        public class M { [CsvOrder(0), CsvConverter(typeof(C))] public int A { get; set; } }
        """, "CSVM008");

    [Test]
    public void ConverterNoDefaultCtor_CSVM008() => AssertDiagnostics("""
        public sealed class C(int x) : CsvConverter<int>
        {
            public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out int result) { result = x; return true; }
        }
        public class M { [CsvOrder(0), CsvConverter(typeof(C))] public int A { get; set; } }
        """, "CSVM008");

    [Test]
    public void ConverterNullableValueType_NoDiagnostic() => AssertDiagnostics("""
        public sealed class C : CsvConverter<int?>
        {
            public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out int? result) { result = 1; return true; }
        }
        public class M { [CsvOrder(0), CsvConverter(typeof(C))] public int? A { get; set; } }
        """);

    [Test]
    public void NoMappedMembers_CSVM010()
    {
        var gen = AssertDiagnostics("""
            public class M { public int A { get; set; } }
            """, "CSVM010");
        Assert.That(gen.Single().Severity, Is.EqualTo(DiagnosticSeverity.Warning));
    }

    [Test]
    public void ModelUsedByTwoMethods_ReportedOnce()
    {
        var (gen, _, _) = GeneratorHarness.Run("""
            using System.Collections.Generic;
            using System.IO;
            using CsvMole;

            [CsvParser]
            public partial class P
            {
                public partial IEnumerable<M> Parse(TextReader reader);
                public partial IEnumerable<M> Parse2(TextReader reader);
            }
            public class M { [CsvOrder(-1)] public int A { get; set; } }
            """);
        Assert.That(gen.Select(d => d.Id), Is.EqualTo(new[] { "CSVM004" }));
    }

    [Test]
    public void UnmappedOptionalCtorParam_NoDiagnostic() => AssertDiagnostics("""
        public class M
        {
            public M([CsvOrder(0)] int a, int b = 5) { A = a + b; }
            public int A { get; }
        }
        """);

    [Test]
    public void UnmappedRequiredCtorParam_CSVM006() => AssertDiagnostics("""
        public class M
        {
            public M([CsvOrder(0)] int a, int b) { A = a + b; }
            public int A { get; }
        }
        """, "CSVM006");

    [TestCase("\"\"")]
    [TestCase("\"  \"")]
    [TestCase("null!")]
    public void EmptyColumnName_CSVM004(string name) => AssertDiagnostics($$"""
        public class M { [CsvColumn({{name}})] public int A { get; set; } }
        """, "CSVM004");

    [Test]
    public void ConflictingOrderOnMemberAndParam_CSVM004() => AssertDiagnostics("""
        public class M
        {
            public M([CsvOrder(1)] int a) { A = a; }
            [CsvOrder(0)] public int A { get; }
        }
        """, "CSVM004");

    [Test]
    public void ConflictingColumnOnMemberAndParam_CSVM004() => AssertDiagnostics("""
        public class M
        {
            public M([CsvColumn("x")] int a) { A = a; }
            [CsvColumn("y")] public int A { get; }
        }
        """, "CSVM004");

    [Test]
    public void SameAttributeOnMemberAndParam_NoDiagnostic() => AssertDiagnostics("""
        public class M
        {
            public M([CsvColumn("id")] int a) { A = a; }
            [CsvColumn("Id")] public int A { get; }
        }
        """);

    [Test]
    public void UndefinedMemberType_NoGeneratorDiagnostic()
    {
        var (gen, comp, _) = GeneratorHarness.Run(Prelude + """
            public class M { [CsvOrder(0)] public Undefined? A { get; set; } }
            """);
        Assert.That(gen, Is.Empty, string.Join("\n", gen));
        Assert.That(comp.Select(d => d.Id), Does.Contain("CS0246"));
    }
}
