namespace DeriSock.DevTools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using DeriSock.DevTools.ApiDoc;
using DeriSock.DevTools.ApiDoc.Analysis;
using DeriSock.DevTools.ApiDoc.Model;
using DeriSock.DevTools.CodeDom;

internal class Program
{
  private const string ApiDocumentationJsonPath = @"C:\Users\psoll\source\repos\DeriSock\DeriSock.DevTools\deribit.api.v211.json";
  private const string GeneratedSourcePath = @"C:\Users\psoll\source\repos\DeriSock\DeriSock.DevTools\Generated";

  public static async Task Main(string[] args)
  {
    try {
      //await CreateApiDocumentationJsonAsync(ApiDocumentationJsonPath);
      //await ApplyApiDocumentationJsonOverridesAsync(ApiDocumentationJsonPath);
      //await AnalyzeApiDocumentationJsonAsync(ApiDocumentationJsonPath);
      await CreateManagedTypeSuggestionsAsync(ApiDocumentationJsonPath);
      //await GenerateSourceAsync(ApiDocumentationJsonPath, GeneratedSourcePath);
      //await Playground();

      Console.WriteLine("Press any key to close ...");
      Console.ReadKey();
    }
    catch (Exception ex) {
      Console.WriteLine(ex.ToString());
      Console.ReadLine();
    }
  }

  private static async Task CreateApiDocumentationJsonAsync(string filePath)
  {
    var apiDoc = await DocumentationBuilder.FromUrlAsync("https://docs.deribit.com");

    var serializedApiDoc = JsonSerializer.Serialize
    (
      apiDoc, new JsonSerializerOptions
      {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
      }
    );

    await File.WriteAllTextAsync(filePath, serializedApiDoc);
  }

  private static async Task ApplyApiDocumentationJsonOverridesAsync(string filePath)
  {
    var apiDoc = await DocumentationBuilder.FromFileAsync(filePath);
    if (apiDoc is null)
      return;

    await DocumentationBuilder.ApplyOverrides(apiDoc, filePath);

    var serializedApiDoc = JsonSerializer.Serialize
    (
      apiDoc, new JsonSerializerOptions
      {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
      }
    );

    await File.WriteAllTextAsync(filePath, serializedApiDoc);
  }

  private static async Task AnalyzeApiDocumentationJsonAsync(string filePath)
  {
    var apiDoc = await DocumentationBuilder.FromFileAsync(filePath);
    if (apiDoc is null)
      return;

    var analyzer = new DocumentationAnalyzer(apiDoc);
    var analyzeResult = analyzer.Analyze();

    var groupedEntries = analyzeResult.GetEntriesGroupedByType();

    foreach (var groupEntry in groupedEntries) {
      Console.WriteLine(new string('-', 75));
      Console.Write("--  ");
      Console.WriteLine(groupEntry.Key);
      Console.WriteLine(new string('-', 75));
      Console.WriteLine();

      var entryCount = 0;
      foreach (var entry in groupEntry) {
        entryCount++;
        foreach (var treeItem in entry.ItemTree) {
          Console.Write(treeItem.Name);

          if (!ReferenceEquals(treeItem, entry.ItemTree!.Last!.Value))
            Console.Write(".");
        }

        Console.WriteLine($" : {entry.Message}");
      }

      Console.WriteLine();
      Console.WriteLine($"-- Count: {entryCount}");
      Console.WriteLine();
    }

    /*
     *  TODO: Next planned step is to change the listing of errors into a scaffolding of override JSON where only the managed type names need to be added
     *  TODO: To go a bit more crazy, first compare all the complex types and group them.
     *  TODO: This enables us to give a suggestion about which managed types really are needed. This can prevent duplicates.
     */
  }

  private static async Task CreateManagedTypeSuggestionsAsync(string filePath)
  {
    var apiDoc = await DocumentationBuilder.FromFileAsync(filePath);
    if (apiDoc is null)
      return;

    var analyzer = new DocumentationAnalyzer(apiDoc);
    var analyzeResult = analyzer.Analyze();

    var missingManagedTypesAnalysisEntries = analyzeResult.GetEntries(AnalysisType.MissingManagedType);
    var groupedEntries = missingManagedTypesAnalysisEntries.Select(entry => entry.ItemTree!.Last!.Value).GroupBy(x => x.GetHashCode()).ToList();

    //groupedEntries.Sort((g1, g2) => g2.First().GetPath().Sum(c => '.'.Equals(c) ? 1 : 0).CompareTo(g1.First().GetPath().Sum(c => '.'.Equals(c) ? 1 : 0)));

    foreach (var group in groupedEntries) {
      Console.WriteLine($"Group: {group.Key}");
      foreach (var response in group) {
        Console.WriteLine(response.ToString());
      }
      Console.WriteLine();
      Console.WriteLine();
    }
  }

  private static async Task GenerateSourceAsync(string documentationJsonPath, string generatedSourcesPath)
  {
    var apiDoc = JsonSerializer.Deserialize<Documentation>(await File.ReadAllTextAsync(documentationJsonPath));

    if (apiDoc is null)
      return;

    var apiCodeProvider = new ApiDocumentationCodeProvider();

    foreach (var (methodName, method) in apiDoc.Methods) {
      var className = methodName.Split('/').ToPublicCodeName();

      await using var fs = new FileStream(Path.Combine(generatedSourcesPath, string.Concat(className, ".cs")), FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

      //apiCodeProvider.GenerateMethodParameters(sw, methodName, method);
      await apiCodeProvider.GenerateMethodResponseAsync(fs, methodName, method);
      await fs.FlushAsync();
    }
  }

  private static async Task Playground()
  {
    var apiDoc = JsonSerializer.Deserialize<Documentation>(await File.ReadAllTextAsync(ApiDocumentationJsonPath));

    if (apiDoc is null)
      return;

    var enums = new Dictionary<string, List<string>>();

    foreach (var (methodName, method) in apiDoc.Methods) {
      if (method.Params is null)
        continue;

      foreach (var (paramName, param) in method.Params) {
        if (param.Enum is null)
          continue;

        if (!enums.ContainsKey(paramName))
          enums[paramName] = new List<string>();

        enums[paramName].AddRange(param.Enum);
      }
    }

    foreach (var kvp in enums) {
      var distinctEnumValues = kvp.Value.Distinct().ToArray();
      kvp.Value.Clear();
      kvp.Value.AddRange(distinctEnumValues);
    }

    var sb = new StringBuilder(300);

    foreach (var (paramName, enumValues) in enums) {
      var className = $"{paramName.SnakeCaseToUpperCamelCase()}Value";

      sb.AppendLine($"public class {className} : EnumValueBase");
      sb.AppendLine("{");

      foreach (var enu in enumValues)
        sb.AppendLine($"  public static readonly {className} {enu.SnakeCaseToUpperCamelCase()} = new(\"{enu}\");");

      sb.AppendLine();
      sb.AppendLine($"  public {className}(string value) : base(value) {{ }}");
      sb.AppendLine("};");
      sb.AppendLine();

      Console.WriteLine(sb.ToString());
      sb.Clear();
    }
  }
}

public abstract class EnumValueBase
{
  private readonly string _value;

  protected EnumValueBase(string value)
  {
    _value = value;
  }

  /// <summary>
  ///   Returns the value of the enum
  /// </summary>
  public override string ToString()
  {
    return _value;
  }
}

public class MyEnumValue : EnumValueBase
{
  public static readonly MyEnumValue Blub = new("blub");

  public MyEnumValue(string value) : base(value) { }
}
