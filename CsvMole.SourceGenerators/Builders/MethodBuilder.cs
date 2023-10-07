using System.Text;

namespace CsvMole.Source.Builders;

internal sealed class MethodBuilder(CsvParserModel model)
{
    private const string ParserMethodName = "Parse";

    public string Build()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"         public partial {model.ReturnType} {ParserMethodName}({model.ParameterType} stringReader)");
        sb.AppendLine("          {");

        // Iterate each line of the StringBuilder
        sb.AppendLine("while (stringReader.ReadLine() is { } line)");
        sb.AppendLine("     {");
        
        sb.AppendLine($"var model = new {model.InnerReturnType}();");
        
        // Split line into values
        sb.AppendLine("var values = line.Split(',');");
        
        // sb.AppendLine("System.Console.WriteLine($\"Got {values.Length} values\");");

        var i = 0;
        foreach ( var property in model.Properties )
        {
            sb.AppendLine($"model.{property.Name} = values[{i}];");
            i++;
        }

        sb.AppendLine("yield return model;");
        
        sb.AppendLine("     }");
        
        // Close while loop
        sb.AppendLine("      }");
        

        return sb.ToString();
    }
}