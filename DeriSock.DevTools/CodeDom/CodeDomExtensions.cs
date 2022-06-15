namespace DeriSock.DevTools.CodeDom;

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using DeriSock.DevTools.ApiDoc.Model;
using DeriSock.Model.Base;

public static class CodeDomExtensions
{
  private static readonly TextInfo EnglishTextInfo = new CultureInfo("en-us", false).TextInfo;

  public static ManagedTypeInfo GetManagedTypeInfo(this Parameter parameter)
  {
    var type = parameter.Type switch
    {
      "number"      => typeof(decimal),
      "float"       => typeof(double),
      "long"        => typeof(long),
      "integer"     => typeof(int),
      "boolean"     => typeof(bool),
      "string"      => typeof(string),
      "object"      => typeof(object),
      "objectArray" => typeof(object),
      "array" => parameter.ArrayType switch
      {
        "number"  => typeof(decimal),
        "float"   => typeof(double),
        "integer" => typeof(int),
        "boolean" => typeof(bool),
        "string"  => typeof(string),
        "object"  => typeof(object),
        _         => throw new ArgumentOutOfRangeException($"Parameter has unknown array type: '{parameter.ArrayType}'")
      },
      _ => throw new ArgumentOutOfRangeException($"Parameter has unknown type: '{parameter.Type}'")
    };

    return new ManagedTypeInfo(type, parameter.IsArray, !parameter.Required);
  }

  public static ManagedTypeInfo GetManagedTypeInfo(this Response response)
  {
    var type = response.Type switch
    {
      "number"      => typeof(decimal),
      "float"       => typeof(double),
      "long"        => typeof(long),
      "integer"     => typeof(int),
      "boolean"     => typeof(bool),
      "string"      => typeof(string),
      "object"      => typeof(object),
      "objectArray" => typeof(object),
      "array" => response.ArrayType switch
      {
        "number"             => typeof(decimal),
        "float"              => typeof(double),
        "integer"            => typeof(int),
        "boolean"            => typeof(bool),
        "string"             => typeof(string),
        "object"             => typeof(object),
        "[timestamp, value]" => typeof(object),
        _                    => throw new ArgumentOutOfRangeException($"Response has unknown array type: '{response.ArrayType}'")
      },
      _ => throw new ArgumentOutOfRangeException($"Response has unknown type: '{response.Type}'")
    };

    return new ManagedTypeInfo(type, response.IsArray, false);
  }

  public static ManagedTypeInfo GetManagedTypeInfo(this ResponseParameter parameter)
  {
    var type = parameter.Type switch
    {
      "number"      => typeof(decimal),
      "float"       => typeof(double),
      "long"        => typeof(long),
      "integer"     => typeof(int),
      "boolean"     => typeof(bool),
      "string"      => typeof(string),
      "object"      => typeof(object),
      "objectArray" => typeof(object),
      "array" => parameter.ArrayType switch
      {
        "number"          => typeof(decimal),
        "float"           => typeof(double),
        "integer"         => typeof(int),
        "long"            => typeof(long),
        "boolean"         => typeof(bool),
        "string"          => typeof(string),
        "object"          => typeof(object),
        "[price, amount]" => typeof(PriceAmount),
        null              => typeof(object),
        _                 => throw new ArgumentOutOfRangeException($"Response object parameter has unknown array type: '{parameter.ArrayType}'")
      },
      _ => throw new ArgumentOutOfRangeException($"Response object parameter has unknown type: '{parameter.Type}'")
    };

    return new ManagedTypeInfo(type, parameter.IsArray, parameter.Optional);
  }

  public static string ToPublicCodeName(this string value)
  {
    return EnglishTextInfo.ToTitleCase(value).Replace("_", string.Empty).Replace(">", string.Empty);
  }

  public static string ToPublicCodeName(this IEnumerable<string> value)
  {
    value = value.Select(w => EnglishTextInfo.ToTitleCase(w));

    var unsafeResult = string.Concat(value);
    return unsafeResult.Replace("_", string.Empty).Replace(">", string.Empty);
  }

  public static string SnakeCaseToUpperCamelCase(this string value)
  {
    return value.Split('_').ToPublicCodeName();
  }

  public static void AddProperty(this CodeTypeMemberCollection value, string fieldName, ManagedTypeInfo propertyTypeInfo, string propertyName, string propertyDescription, bool? isDeprecated)
  {
    // Define type nullability and initialization
    var typeSuffix = propertyTypeInfo.IsNullable ? "?" : string.Empty;
    var typeInitialization = string.Empty;

    if (!propertyTypeInfo.IsNullable) {
      if (propertyTypeInfo.IsArray) {
        typeInitialization = $" = System.Array.Empty<{propertyTypeInfo.Type.FullName}>();";
      }
      else {
        if (propertyTypeInfo.Type == typeof(string)) {
          typeInitialization = " = string.Empty;";
        }
        else if (propertyTypeInfo.Type == typeof(object)) {
          typeInitialization = " = null!;";
        }
        else {
          typeInitialization = " = default;";
        }
      }
    }

    var propertyCodeString = new StringBuilder();

    // Add Attributes
    if (isDeprecated.HasValue && isDeprecated.Value)
      propertyCodeString.AppendLine("    [System.ObsoleteAttribute()]");

    propertyCodeString.AppendLine($"    [Newtonsoft.Json.JsonPropertyAttribute(\"{fieldName}\")]");

    // Create the property
    propertyCodeString.Append($"    public {(propertyTypeInfo.IsArray ? propertyTypeInfo.Type.MakeArrayType() : propertyTypeInfo.Type)}{typeSuffix} {propertyName} {{ get; set; }}{typeInitialization}");
    var codeProperty = new CodeSnippetTypeMember(propertyCodeString.ToString());

    // Add Comments to the property
    codeProperty.Comments.Add(new CodeCommentStatement("<summary>", true));

    foreach (var xmlDocParagraph in propertyDescription.ToXmlDocParagraphs()) {
      codeProperty.Comments.Add(new CodeCommentStatement($"<para>{xmlDocParagraph}</para>", true));
    }

    codeProperty.Comments.Add(new CodeCommentStatement("</summary>", true));
    
    value.Add(codeProperty);
  }
}
