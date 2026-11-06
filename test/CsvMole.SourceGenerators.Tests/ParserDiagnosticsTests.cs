using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CsvMole.SourceGenerators.Tests;

public class ParserDiagnosticsTests
{
    private const string Usings = """
        using System.Collections.Generic;
        using System.IO;
        using System.Threading;
        using CsvMole;

        public sealed class Model { [CsvColumn("name")] public string? Name { get; set; } }

        """;

    private static string[] Ids(IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Select(d => d.Id).Distinct().Order().ToArray();

    private static void AssertDiagnostics(string body, params string[] expectedIds)
    {
        var (gen, comp, _) = GeneratorHarness.Run(Usings + body);
        Assert.That(Ids(gen), Is.EqualTo(expectedIds));
        Assert.That(comp, Is.Empty, string.Join("\n", comp));
    }

    // Ruling L: for CSVM001 the generator emits nothing for the class, so the compiler's own
    // CS0751/CS8795 are expected alongside; only the generator diagnostics are asserted.
    [Test]
    public void NonPartialClass_CSVM001()
    {
        var (gen, _, _) = GeneratorHarness.Run(Usings + """
            [CsvParser]
            public class P { public partial IEnumerable<Model> Parse(TextReader reader); }
            """);
        Assert.That(Ids(gen), Is.EqualTo(new[] { "CSVM001" }));
    }

    [TestCase("partial record P")]
    [TestCase("partial struct P")]
    [TestCase("partial interface P")]
    public void NonClassParser_CSVM001(string declaration)
    {
        var (gen, _, _) = GeneratorHarness.Run(Usings + $"[CsvParser] public {declaration} {{ public partial IEnumerable<Model> Parse(TextReader reader); }}");
        Assert.That(Ids(gen), Is.EqualTo(new[] { "CSVM001" }));
    }

    [Test]
    public void GenericParserClass_CSVM001()
    {
        var (gen, _, _) = GeneratorHarness.Run(Usings + """
            [CsvParser]
            public partial class P<T> { public partial IEnumerable<Model> Parse(TextReader reader); }
            """);
        Assert.That(Ids(gen), Is.EqualTo(new[] { "CSVM001" }));
    }

    [Test]
    public void FileLocalParserClass_CSVM001()
    {
        var (gen, _, _) = GeneratorHarness.Run(Usings + """
            [CsvParser]
            file partial class P { public partial IEnumerable<Model> Parse(TextReader reader); }
            """);
        Assert.That(Ids(gen), Is.EqualTo(new[] { "CSVM001" }));
    }

    [Test]
    public void ParserInFileLocalContainingType_CSVM001()
    {
        var (gen, _, _) = GeneratorHarness.Run(Usings + """
            file partial class Outer
            {
                [CsvParser]
                public partial class P { public partial IEnumerable<Model> Parse(TextReader reader); }
            }
            """);
        Assert.That(Ids(gen), Is.EqualTo(new[] { "CSVM001" }));
    }

    [Test]
    public void ExtensionMethodParser_NoDiagnostics() => AssertDiagnostics("""
        [CsvParser]
        public static partial class P { public static partial IEnumerable<Model> Parse(this TextReader reader); }
        """);

    [Test]
    public void OptionalVoidPartialMethod_Ignored() => AssertDiagnostics("""
        [CsvParser]
        public partial class P
        {
            partial void OnRead();
            public partial IEnumerable<Model> Parse(TextReader reader, CsvOptions? options = null);
        }
        """);

    [Test]
    public void WrongReturnType_CSVM002() => AssertDiagnostics("""
        [CsvParser]
        public partial class P { public partial List<Model> Parse(TextReader reader); }
        """, "CSVM002");

    [Test]
    public void FirstParamNotTextReader_CSVM002() => AssertDiagnostics("""
        [CsvParser]
        public partial class P { public partial IEnumerable<Model> Parse(string text); }
        """, "CSVM002");

    [TestCase("StringReader")]
    [TestCase("StreamReader")]
    public void FirstParamTextReaderSubclass_NoDiagnostics(string readerType) => AssertDiagnostics($$"""
        [CsvParser]
        public partial class P { public partial IEnumerable<Model> Parse({{readerType}} reader, CsvOptions? options = null); }
        """);

    [Test]
    public void FirstParamNotTextReader_MessageMentionsDerivedTypes()
    {
        var (gen, _, _) = GeneratorHarness.Run(Usings + """
            [CsvParser]
            public partial class P { public partial IEnumerable<Model> Parse(object reader); }
            """);
        Assert.That(gen.Single().GetMessage(),
            Does.Contain("the first parameter must be a System.IO.TextReader or a type derived from it"));
    }

    [Test]
    public void ExtraUnknownParam_CSVM002() => AssertDiagnostics("""
        [CsvParser]
        public partial class P { public partial IEnumerable<Model> Parse(TextReader reader, int count = 3); }
        """, "CSVM002");

    [Test]
    public void CancellationTokenOnSync_CSVM002() => AssertDiagnostics("""
        [CsvParser]
        public partial class P { public partial IEnumerable<Model> Parse(TextReader reader, CancellationToken ct); }
        """, "CSVM002");

    [Test]
    public void GenericMethod_CSVM002_StubMatchesConstraints() => AssertDiagnostics("""
        [CsvParser]
        public partial class P { public static partial IEnumerable<T> Parse<T>(TextReader reader) where T : class, new(); }
        """, "CSVM002");

    [Test]
    public void AsyncWithoutToken_CSVM009()
    {
        var (gen, comp, _) = GeneratorHarness.Run(Usings + """
            [CsvParser]
            public partial class P { public partial IAsyncEnumerable<Model> ParseAsync(TextReader reader, CsvOptions? options = null); }
            """);
        Assert.That(gen.Select(d => (d.Id, d.Severity)), Is.EqualTo(new[] { ("CSVM009", DiagnosticSeverity.Warning) }));
        Assert.That(comp, Is.Empty, string.Join("\n", comp));
    }

    [Test]
    public void NestedInternalParser_NoDiagnostics()
    {
        var (gen, comp, driver) = GeneratorHarness.Run(Usings + """
            namespace My.App
            {
                internal static partial class Outer
                {
                    [CsvParser]
                    internal partial class P
                    {
                        public partial IEnumerable<Model> Parse(TextReader reader, CsvOptions? options = null);
                        internal partial IAsyncEnumerable<Model> ParseAsync(TextReader reader, CsvOptions? options = null, CancellationToken cancellationToken = default);
                    }
                }
            }
            """);
        Assert.That(gen, Is.Empty);
        Assert.That(comp, Is.Empty, string.Join("\n", comp));
        var generated = driver.GetRunResult().GeneratedTrees.Single();
        Assert.That(generated.FilePath, Does.EndWith("My.App.Outer.P.g.cs"));
        TestContext.Out.WriteLine(generated.ToString());
    }

    [Test]
    public void ParameterNamesRespected()
    {
        var (gen, comp, driver) = GeneratorHarness.Run(Usings + """
            [CsvParser]
            public partial class P { public partial IEnumerable<Model> Parse(TextReader input, CsvOptions? opts); }
            """);
        Assert.That(gen, Is.Empty);
        Assert.That(comp, Is.Empty, string.Join("\n", comp));
        var generated = driver.GetRunResult().GeneratedTrees.Single().ToString();
        Assert.That(generated, Does.Contain("global::System.IO.TextReader input, global::CsvMole.CsvOptions? opts"));
        Assert.That(generated, Does.StartWith("// <auto-generated/>"));
        Assert.That(generated, Does.Contain("#nullable enable"));
        Assert.That(generated, Does.Contain("[global::System.CodeDom.Compiler.GeneratedCode(\"CsvMole\", \""));
        Assert.That(generated, Does.Contain("var __options = opts ?? global::CsvMole.CsvOptions.Default;"));
        Assert.That(generated, Does.Contain("new global::CsvMole.CsvReader(input, __options,"));
        TestContext.Out.WriteLine(generated);
    }

    [TestCase("partial class Outer<T>")]
    [TestCase("class Outer")]
    public void ParserInGenericOrNonPartialContainingType_CSVM001(string outer)
    {
        var (gen, _, _) = GeneratorHarness.Run(Usings + $$"""
            public {{outer}}
            {
                [CsvParser]
                public partial class P { public partial IEnumerable<Model> Parse(TextReader reader); }
            }
            """);
        Assert.That(Ids(gen), Is.EqualTo(new[] { "CSVM001" }));
    }

    [TestCase("partial record Outer")]
    [TestCase("partial record struct Outer")]
    [TestCase("partial struct Outer")]
    public void ParserNestedInRecordOrStruct_Compiles(string outer)
    {
        var (gen, comp, _) = GeneratorHarness.Run(Usings + $$"""
            public {{outer}}
            {
                [CsvParser]
                public partial class P { public partial IEnumerable<Model> Parse(TextReader reader); }
            }
            """);
        Assert.That(gen, Is.Empty, string.Join("\n", gen));
        Assert.That(comp, Is.Empty, string.Join("\n", comp));
    }

    // Void partials the compiler requires to be implemented are parser methods, so they get CSVM002.
    [TestCase("public partial void Parse(TextReader reader);")]
    [TestCase("private partial void Parse(TextReader reader);")]
    public void VoidPartialRequiringImplementation_CSVM002(string method) => AssertDiagnostics($$"""
        [CsvParser]
        public partial class P { {{method}} }
        """, "CSVM002");

    // The compiler also reports CS8797 (out params need an accessibility modifier); only the generator side is asserted.
    [Test]
    public void VoidPartialWithOutParam_CSVM002()
    {
        var (gen, _, _) = GeneratorHarness.Run(Usings + """
            [CsvParser]
            public partial class P { partial void Parse(TextReader reader, out int count); }
            """);
        Assert.That(Ids(gen), Is.EqualTo(new[] { "CSVM002" }));
    }

    [TestCase("public partial IEnumerable<Model> Parse();")]
    [TestCase("public partial IEnumerable<Model> Parse(ref TextReader reader);")]
    [TestCase("public partial IEnumerable<Model> Parse(TextReader reader, CsvOptions a, CsvOptions b);")]
    [TestCase("public partial IAsyncEnumerable<Model> Parse(TextReader reader, CancellationToken a, CancellationToken b);")]
    [TestCase("public partial IAsyncEnumerable<Model> Parse(TextReader reader, ref CancellationToken token);")]
    [TestCase("public partial Model[] Parse(TextReader reader);")]
    [TestCase("public partial string Parse(TextReader reader);")]
    public void InvalidSignature_CSVM002(string method) => AssertDiagnostics($$"""
        [CsvParser]
        public partial class P { {{method}} }
        """, "CSVM002");

    [Test]
    public void ModelsWithClashingSanitisedNames_Compile()
    {
        // A.B_C and A_B.C both sanitise to A_B_C; the generated helper names must still be distinct.
        var (gen, comp, _) = GeneratorHarness.Run(Usings + """
            namespace A { public class B_C { [CsvOrder(0)] public int X { get; set; } } }
            namespace A_B { public class C { [CsvOrder(0)] public string? Y { get; set; } } }

            [CsvParser]
            public partial class P
            {
                public partial IEnumerable<A.B_C> ParseFirst(TextReader reader);
                public partial IEnumerable<A_B.C> ParseSecond(TextReader reader);
            }
            """);
        Assert.That(gen, Is.Empty, string.Join("\n", gen));
        Assert.That(comp, Is.Empty, string.Join("\n", comp));
    }

    [Test]
    public void PipelineIsCacheable()
    {
        var compilation = GeneratorHarness.CreateCompilation(Usings + """
            namespace N
            {
                [CsvParser]
                public partial class P
                {
                    public partial IEnumerable<Model> Parse(TextReader reader, CsvOptions? options = null);
                    public partial List<Model> Bad(TextReader reader);
                }
            }
            """);
        var driver = GeneratorHarness.CreateDriver().RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("public class Unrelated { }", GeneratorHarness.ParseOptions)));

        var steps = driver.GetRunResult().Results.Single().TrackedSteps;
        Assert.That(steps.Keys, Does.Contain(CsvParserGenerator.ParserModelStep));
        var reasons = steps[CsvParserGenerator.ParserModelStep].SelectMany(s => s.Outputs).Select(o => o.Reason).ToArray();
        Assert.That(reasons, Is.Not.Empty);
        Assert.That(reasons, Is.All.AnyOf(IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged));
    }
}
