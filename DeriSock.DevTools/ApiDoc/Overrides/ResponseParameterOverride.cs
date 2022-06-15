namespace DeriSock.DevTools.ApiDoc.Overrides;

using System.Text.Json.Serialization;

using DeriSock.DevTools.ApiDoc.Model;

public class ResponseParameterOverride
{
  [JsonPropertyName("name")]
  public string? Name { get; set; }
  
  [JsonPropertyName("type")]
  public string? Type { get; set; }
  
  [JsonPropertyName("arrayType")]
  public string? ArrayType { get; set; }

  [JsonPropertyName("description")]
  public string? Description { get; set; }

  [JsonPropertyName("deprecated")]
  public bool? Deprecated { get; set; }

  [JsonPropertyName("optional")]
  public bool? Optional { get; set; }

  [JsonPropertyName("managedType")]
  public ManagedTypeData? ManagedType { get; set; }

  [JsonPropertyName("objectParams")]
  public ResponseParameterOverrideCollection? ObjectParams { get; set; }
}
