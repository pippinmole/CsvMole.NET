using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CsvMole.SourceGenerators.Tests;

internal static partial class Snapshot
{
    [GeneratedRegex("""GeneratedCode\("CsvMole", "[^"]*"\)""")]
    private static partial Regex Version();

    public static void Match(string generatedSource, [CallerMemberName] string name = "", [CallerFilePath] string file = "")
    {
        var dir = Path.Combine(Path.GetDirectoryName(file)!, "Snapshots");
        var verified = Path.Combine(dir, name + ".verified.cs");
        var received = Path.Combine(dir, name + ".received.cs");
        var actual = Version().Replace(generatedSource.ReplaceLineEndings("\n"), """GeneratedCode("CsvMole", "VERSION")""");

        if (File.Exists(verified) && File.ReadAllText(verified).ReplaceLineEndings("\n") == actual)
        {
            File.Delete(received);
            return;
        }
        Directory.CreateDirectory(dir);
        File.WriteAllText(received, actual);
        Assert.Fail($"Snapshot mismatch or missing.\n  verified: {verified}\n  received: {received}");
    }
}
