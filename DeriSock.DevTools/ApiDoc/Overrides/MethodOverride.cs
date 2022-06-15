namespace DeriSock.DevTools.ApiDoc.Overrides;

using System.Text.Json.Serialization;

public class MethodOverride
{
  [JsonPropertyName("scope")]
  public string Scope { get; set; } = null!;

  [JsonPropertyName("name")]
  public string Name { get; set; } = null!;

  [JsonPropertyName("description")]
  public string? Description { get; set; } = null!;

  [JsonPropertyName("deprecated")]
  public bool? Deprecated { get; set; }

  [JsonPropertyName("params")]
  public ParameterOverrideCollection? Params { get; set; }

  [JsonPropertyName("response")]
  public ResponseOverride? Response { get; set; }
}
