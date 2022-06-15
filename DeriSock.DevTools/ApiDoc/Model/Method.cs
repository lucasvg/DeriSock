namespace DeriSock.DevTools.ApiDoc.Model;

using System.Text.Json.Serialization;

public class Method : IDocumentationNode
{
  [JsonIgnore]
  public IDocumentationNode? Parent { get; set; }

  [JsonIgnore]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("category")]
  public string Category { get; set; } = null!;

  [JsonPropertyName("description")]
  public string Description { get; set; } = null!;

  [JsonPropertyName("deprecated")]
  public bool? Deprecated { get; set; }

  [JsonPropertyName("params")]
  public ParameterCollection? Params { get; set; }

  [JsonPropertyName("response")]
  public Response? Response { get; set; }

  public override string ToString()
  {
    return this.GetPath();
  }
}
