using CsvMole.SourceGenerators.Analysis;
using CsvMole.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace CsvMole.SourceGenerators.Tests;

public class ModelAnalyzerTests
{
    private static (ModelInfo? Model, List<DiagnosticInfo> Diagnostics) Analyze(string source, string typeName = "M")
    {
        var compilation = GeneratorHarness.CreateCompilation("using System; using CsvMole;\n" + source);
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
        var diagnostics = new List<DiagnosticInfo>();
        var model = ModelAnalyzer.Analyze(compilation.GetTypeByMetadataName(typeName)!, compilation, diagnostics);
        return (model, diagnostics);
    }

    private static ModelInfo AnalyzeValid(string source)
    {
        var (model, diagnostics) = Analyze(source);
        Assert.That(diagnostics.Select(d => d.Id), Is.Empty);
        return model!;
    }

    private static MemberInfo Member(ModelInfo model, string name) => model.Members.AsSpan().ToArray().Single(m => m.Name == name);

    [Test]
    public void Record_PrimaryCtor_PropertyTargetAttributes()
    {
        var model = AnalyzeValid("""public record M([property: CsvColumn("name")] string Name, [property: CsvOrder(1)] int Age);""");
        Assert.That(model.TypeFullName, Is.EqualTo("global::M"));
        Assert.That(model.Members.AsSpan().Length, Is.EqualTo(2));
        Assert.That(model.Constructor.ParameterTypeFullNames.AsSpan().ToArray(), Is.EqualTo(new[] { "string", "int" }));

        var name = Member(model, "Name");
        Assert.That((name.Assignment, name.CtorParameterIndex, name.ColumnName, name.Order, name.Kind),
            Is.EqualTo((MemberAssignment.Constructor, 0, "name", -1, ParseKind.String)));
        var age = Member(model, "Age");
        Assert.That((age.Assignment, age.CtorParameterIndex, age.ColumnName, age.Order, age.Kind, age.UnderlyingTypeFullName),
            Is.EqualTo((MemberAssignment.Constructor, 1, (string?)null, 1, ParseKind.SpanParsable, "int")));
    }

    [Test]
    public void AttributeOnCtorParameter()
    {
        var model = AnalyzeValid("""
            public class M
            {
                public M([CsvOrder(0)] int id) { Id = id; }
                public int Id { get; }
            }
            """);
        var id = Member(model, "Id");
        Assert.That((id.Assignment, id.CtorParameterIndex, id.Order), Is.EqualTo((MemberAssignment.Constructor, 0, 0)));
    }

    [Test]
    public void CtorPlusInitializer()
    {
        var model = AnalyzeValid("""
            public class M
            {
                public M(int id) { Id = id; }
                [CsvOrder(0)] public int Id { get; }
                [CsvOrder(1)] public string? Name { get; init; }
            }
            """);
        Assert.That(Member(model, "Id").Assignment, Is.EqualTo(MemberAssignment.Constructor));
        var name = Member(model, "Name");
        Assert.That((name.Assignment, name.CtorParameterIndex), Is.EqualTo((MemberAssignment.Initializer, -1)));
    }

    [Test]
    public void UnannotatedMemberIgnored()
    {
        var model = AnalyzeValid("public class M { [CsvOrder(0)] public int A { get; set; } public int B { get; set; } }");
        Assert.That(model.Members.AsSpan().ToArray().Select(m => m.Name), Is.EqualTo(new[] { "A" }));
        Assert.That(model.Constructor.ParameterTypeFullNames.IsEmpty, Is.True);
    }

    [Test]
    public void InheritedAndFieldMembers()
    {
        var model = AnalyzeValid("""
            public class B { [CsvOrder(0)] public int A { get; set; } }
            public class M : B { [CsvOrder(1)] public int F; }
            """);
        Assert.That(model.Members.AsSpan().ToArray().Select(m => m.Name), Is.EquivalentTo(new[] { "A", "F" }));
    }

    [Test]
    public void NullableKinds()
    {
        var model = AnalyzeValid("""
            public enum Color { Red }
            public class M
            {
                [CsvOrder(0)] public int? A { get; set; }
                [CsvOrder(1)] public string? S { get; set; }
                [CsvOrder(2)] public Color C { get; set; }
                [CsvOrder(3)] public Color? NC { get; set; }
                [CsvOrder(4)] public string T { get; set; } = "";
            }
            """);
        var a = Member(model, "A");
        Assert.That((a.Kind, a.IsNullable, a.UnderlyingTypeFullName, a.TypeFullName), Is.EqualTo((ParseKind.SpanParsable, true, "int", "int?")));
        Assert.That(Member(model, "S").Kind, Is.EqualTo(ParseKind.NullableString));
        Assert.That(Member(model, "T").Kind, Is.EqualTo(ParseKind.String));
        var c = Member(model, "C");
        Assert.That((c.Kind, c.IsNullable, c.UnderlyingTypeFullName), Is.EqualTo((ParseKind.Enum, false, "global::Color")));
        var nc = Member(model, "NC");
        Assert.That((nc.Kind, nc.IsNullable, nc.UnderlyingTypeFullName), Is.EqualTo((ParseKind.Enum, true, "global::Color")));
    }

    [Test]
    public void ObliviousString_IsString()
    {
        var model = AnalyzeValid("#nullable disable\npublic class M { [CsvOrder(0)] public string S { get; set; } }");
        Assert.That(Member(model, "S").Kind, Is.EqualTo(ParseKind.String));
    }

    [Test]
    public void Converter()
    {
        var model = AnalyzeValid("""
            namespace N
            {
                public sealed class C : CsvConverter<int>
                {
                    public override bool TryParse(ReadOnlySpan<char> v, System.Globalization.CultureInfo c, out int r) { r = 0; return true; }
                }
            }
            public class M { [CsvOrder(0), CsvConverter(typeof(N.C))] public int A { get; set; } }
            """);
        var a = Member(model, "A");
        Assert.That((a.Kind, a.ConverterTypeFullName), Is.EqualTo((ParseKind.Converter, "global::N.C")));
    }

    [Test]
    public void StructModel()
    {
        var model = AnalyzeValid("public struct M { [CsvOrder(0)] public int A { get; set; } }");
        Assert.That(model.Constructor.ParameterTypeFullNames.IsEmpty, Is.True);
        Assert.That(Member(model, "A").Assignment, Is.EqualTo(MemberAssignment.Initializer));
    }

    [Test]
    public void DelimiterFromAttribute()
    {
        Assert.That(AnalyzeValid("[CsvDelimiter(';')] public class M { [CsvOrder(0)] public int A { get; set; } }").Delimiter, Is.EqualTo(';'));
        Assert.That(AnalyzeValid("public class M { [CsvOrder(0)] public int A { get; set; } }").Delimiter, Is.Null);
    }

    [TestCase("bool")] [TestCase("char")] [TestCase("sbyte")] [TestCase("byte")] [TestCase("short")] [TestCase("ushort")]
    [TestCase("int")] [TestCase("uint")] [TestCase("long")] [TestCase("ulong")] [TestCase("nint")] [TestCase("nuint")]
    [TestCase("Int128")] [TestCase("UInt128")] [TestCase("float")] [TestCase("double")] [TestCase("decimal")]
    [TestCase("DateTime")] [TestCase("DateTimeOffset")] [TestCase("DateOnly")] [TestCase("TimeOnly")]
    [TestCase("TimeSpan")] [TestCase("Guid")]
    public void BuiltInTypes_AreSpanParsable(string type)
    {
        var model = AnalyzeValid($"public class M {{ [CsvOrder(0)] public {type} A {{ get; set; }} }}");
        Assert.That(Member(model, "A").Kind, Is.EqualTo(ParseKind.SpanParsable));
    }

    [Test]
    public void ModelWithErrors_ReturnsNull()
    {
        var (model, diagnostics) = Analyze("public class M { [CsvOrder(-1)] public int A { get; set; } }");
        Assert.That(model, Is.Null);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "CSVM004" }));
    }

    [Test]
    public void OptionalMiddleParam_ParameterNamesAvailable()
    {
        var model = AnalyzeValid("""
            public class M
            {
                public M([CsvOrder(0)] int a, int b = 0, [CsvOrder(1)] int c = 0) { }
            }
            """);
        Assert.That(model.Constructor.ParameterNames.ToArray(), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(model.Members.ToArray().Select(m => (m.Name, model.Constructor.ParameterNames[m.CtorParameterIndex])),
            Is.EqualTo(new[] { ("a", "a"), ("c", "c") }));
    }

    [Test]
    public void RequiredFlags()
    {
        var sets = AnalyzeValid("""
            public class M
            {
                [System.Diagnostics.CodeAnalysis.SetsRequiredMembers] public M(int id) { Id = id; }
                [CsvOrder(0)] public required int Id { get; init; }
            }
            """);
        Assert.That((Member(sets, "Id").IsRequired, sets.Constructor.SetsRequiredMembers), Is.EqualTo((true, true)));

        var plain = AnalyzeValid("""
            public class M
            {
                public M(int id) { Id = id; }
                [CsvOrder(0)] public required int Id { get; init; }
                [CsvOrder(1)] public int Other { get; set; }
            }
            """);
        Assert.That((Member(plain, "Id").IsRequired, Member(plain, "Id").Assignment, plain.Constructor.SetsRequiredMembers),
            Is.EqualTo((true, MemberAssignment.Constructor, false)));
        Assert.That(Member(plain, "Other").IsRequired, Is.False);
    }

    [Test]
    public void OverrideInheritsBaseAttributes()
    {
        var model = AnalyzeValid("""
            public class B { [CsvColumn("a")] public virtual int A { get; set; } }
            public class M : B { public override int A { get; set; } }
            """);
        Assert.That(Member(model, "A").ColumnName, Is.EqualTo("a"));
    }

    [Test]
    public void ConverterMember_NullableType_IsNullable()
    {
        var model = AnalyzeValid("""
            using System.Globalization;
            public sealed class IntConv : CsvConverter<int?> { public override bool TryParse(ReadOnlySpan<char> v, CultureInfo c, out int? r) { r = null; return true; } }
            public sealed class StrConv : CsvConverter<string?> { public override bool TryParse(ReadOnlySpan<char> v, CultureInfo c, out string? r) { r = null; return true; } }
            public sealed class PlainConv : CsvConverter<int> { public override bool TryParse(ReadOnlySpan<char> v, CultureInfo c, out int r) { r = 0; return true; } }
            public class M
            {
                [CsvOrder(0), CsvConverter(typeof(IntConv))] public int? A { get; set; }
                [CsvOrder(1), CsvConverter(typeof(StrConv))] public string? B { get; set; }
                [CsvOrder(2), CsvConverter(typeof(PlainConv))] public int C { get; set; }
            }
            """);
        Assert.That((Member(model, "A").Kind, Member(model, "A").IsNullable), Is.EqualTo((ParseKind.Converter, true)));
        Assert.That(Member(model, "B").IsNullable, Is.True);
        Assert.That(Member(model, "C").IsNullable, Is.False);
    }
}
