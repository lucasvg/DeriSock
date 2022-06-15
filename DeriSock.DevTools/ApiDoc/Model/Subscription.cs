namespace DeriSock.DevTools.ApiDoc.Model;

using System.Text.Json.Serialization;

public class Subscription : IDocumentationNode
{
  [JsonIgnore]
  public IDocumentationNode? Parent { get; set; }

  [JsonIgnore]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("description")]
  public string Description { get; set; } = null!;

  [JsonPropertyName("params")]
  public ParameterCollection? Params { get; set; }

  [JsonPropertyName("response")]
  public Response? Response { get; set; }

  public override string ToString()
  {
    return this.GetPath();
  }
}
