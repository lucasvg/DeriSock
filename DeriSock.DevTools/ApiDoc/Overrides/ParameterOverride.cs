namespace DeriSock.DevTools.ApiDoc.Overrides;

using System.Text.Json.Serialization;

using DeriSock.DevTools.ApiDoc.Model;

public class ParameterOverride
{
  [JsonPropertyName("name")]
  public string? Name { get; set; }

  [JsonPropertyName("required")]
  public bool? Required { get; set; }

  [JsonPropertyName("type")]
  public string? Type { get; set; } = null!;

  [JsonPropertyName("arrayType")]
  public string? ArrayType { get; set; }

  [JsonPropertyName("enum")]
  public string[]? Enum { get; set; }

  [JsonPropertyName("default")]
  public object? Default { get; set; }

  [JsonPropertyName("maxLength")]
  public int? MaxLength { get; set; }

  [JsonPropertyName("description")]
  public string? Description { get; set; } = null!;

  [JsonPropertyName("managedType")]
  public ManagedTypeData? ManagedType { get; set; }

  [JsonPropertyName("objectArrayParams")]
  public ParameterOverrideCollection? ObjectArrayParams { get; set; }
}
