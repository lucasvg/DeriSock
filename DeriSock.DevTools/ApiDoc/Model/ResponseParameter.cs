namespace DeriSock.DevTools.ApiDoc.Model;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

public class ResponseParameter : IDocumentationNode
{
  [JsonIgnore]
  public IDocumentationNode? Parent { get; set; }

  [JsonIgnore]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("type")]
  public string Type { get; set; } = string.Empty;

  [JsonPropertyName("arrayType")]
  public string? ArrayType { get; set; }

  [JsonPropertyName("description")]
  public string Description { get; set; } = string.Empty;

  [JsonPropertyName("deprecated")]
  public bool Deprecated { get; set; } = false;

  [JsonPropertyName("optional")]
  public bool Optional { get; set; } = false;

  [JsonPropertyName("managedType")]
  public ManagedTypeData? ManagedType { get; set; }

  [JsonPropertyName("objectParams")]
  public ResponseParameterCollection? ObjectParams { get; set; }

  [JsonIgnore]
  public bool IsComplex => Type is "object" or "objectArray" || ArrayType is "object" or "objectArray";

  [JsonIgnore]
  public bool IsArray => Type is "array" or "objectArray";

  [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
  public override int GetHashCode()
  {
    return HashCode.Combine(Name, Type, ArrayType, Description, Deprecated, Optional, ObjectParams);
  }

  public override string ToString()
  {
    return this.GetPath();
  }
}
