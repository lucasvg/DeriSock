namespace DeriSock.DevTools.ApiDoc.Model;

using System.Text.Json.Serialization;

public class ManagedTypeData
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("implements")]
  public string[]? Implements { get; set; }
}
