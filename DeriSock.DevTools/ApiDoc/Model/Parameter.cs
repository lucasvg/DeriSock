namespace DeriSock.DevTools.ApiDoc.Model;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

public class Parameter : IDocumentationNode
{
  [JsonIgnore]
  public IDocumentationNode? Parent { get; set; }

  [JsonIgnore]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("required")]
  public bool Required { get; set; }

  [JsonPropertyName("type")]
  public string Type { get; set; } = null!;

  [JsonPropertyName("arrayType")]
  public string? ArrayType { get; set; }

  [JsonPropertyName("enum")]
  public string[]? Enum { get; set; }

  [JsonPropertyName("default")]
  public object? Default { get; set; }

  [JsonPropertyName("maxLength")]
  public int? MaxLength { get; set; }

  [JsonPropertyName("description")]
  public string Description { get; set; } = null!;

  [JsonPropertyName("managedType")]
  public ManagedTypeData? ManagedType { get; set; }

  [JsonPropertyName("objectArrayParams")]
  public ParameterCollection? ObjectArrayParams { get; set; }

  [JsonIgnore]
  public bool IsComplex => Type is "object" or "objectArray" || ArrayType is "object" or "objectArray";

  [JsonIgnore]
  public bool IsArray => Type is "array" or "objectArray";

  [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
  public override int GetHashCode()
  {
    var hash1 = HashCode.Combine(Name, Required, Type, ArrayType, Enum);
    var hash2 = HashCode.Combine(Default, MaxLength, Description, ObjectArrayParams);
    return HashCode.Combine(hash1, hash2);
  }

  public override string ToString()
  {
    return this.GetPath();
  }
}
