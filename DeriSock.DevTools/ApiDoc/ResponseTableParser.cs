namespace DeriSock.DevTools.ApiDoc;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using DeriSock.DevTools.ApiDoc.Model;

using HtmlAgilityPack;

internal static class ResponseTableParser
{
  private const string ArrayTypeMarker = "array of ";
  private const string ObjectArrayParamMarker = "&nbsp;&nbsp;&rsaquo;&nbsp;&nbsp;";

  private static readonly Regex[] DeprecatedParamPattern =
  {
    new(@"\(field is deprecated and will be removed in the future\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"^\[DEPRECATED\] ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled)
  };

  private static readonly Regex[] OptionalParamPattern =
  {
    new(@"^Optional .+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"^Optional, .+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"\(available when ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"\(optional\)[\.]?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"\. Optional field$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@", optional for ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"\(optional, only for ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"field is omitted", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"only for ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"\. Only when ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"\(options only\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"Field not included if", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@" only\)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled)

  };

  public static Response Parse(HtmlNode tableNode, string dataObjectPropertyName)
  {
    var (resultNode, isComplex, isArray, typeName, description) = FindDataObjectEntry(tableNode, dataObjectPropertyName);

    return new Response
    {
      Type = isArray ? "array" : typeName,
      ArrayType = isArray ? typeName : null,
      Description = description,
      ObjectParams = isComplex ? ParseComplexType(resultNode, 1) : null
    };
  }

  private static (HtmlNode resultNode, bool isComplex, bool isArray, string typeName, string description) FindDataObjectEntry(HtmlNode tableNode, string dataObjectPropertyName)
  {
    tableNode = tableNode.FirstChild.NextSibling;
    var responseTableRows = tableNode.SelectNodes("tr");

    HtmlNode? resultPropertyRow = null;
    HtmlNode[]? resultPropertyRowColumns = null;

    (resultPropertyRow, resultPropertyRowColumns) = FindRowWithName(responseTableRows, dataObjectPropertyName, resultPropertyRow, resultPropertyRowColumns);

    if (resultPropertyRowColumns is null && dataObjectPropertyName == "result")
    {
      // In 'private/get_user_trades_by_order' the result row is kind of missing
      // The amount property is half in the row as "array of | amount | number"
      // Seems as if 'result | array of object' was somehow cut
      (resultPropertyRow, resultPropertyRowColumns) = FindRowWithName(responseTableRows, "array of", resultPropertyRow, resultPropertyRowColumns);
      TryRepairTableIfNeeded(ref resultPropertyRow, ref resultPropertyRowColumns);
    }

    Debug.Assert(resultPropertyRow is not null && resultPropertyRowColumns is not null, $"Response without a {dataObjectPropertyName} row");

    var colValueType = resultPropertyRowColumns[1].InnerText;
    var description = resultPropertyRowColumns[2].ToMarkdown();

    var isArray = colValueType.StartsWith(ArrayTypeMarker);
    var typeName = isArray ? colValueType[ArrayTypeMarker.Length..] : colValueType;

    if (typeName.Equals("objects", StringComparison.OrdinalIgnoreCase))
      typeName = "object";

    if (description.Contains("milliseconds since the UNIX epoch"))
      typeName = "long";

    return (resultPropertyRow, typeName.Equals("object"), isArray, typeName, description);
  }

  private static ResponseParameterCollection ParseComplexType(HtmlNode resultNode, int level)
  {
    var props = new ResponseParameterCollection(10);
    var resultSiblings = new List<HtmlNode>();

    for (var row = resultNode.NextSibling; row is not null; row = row.NextSibling)
    {
      if (row.Name != "tr")
        continue;

      resultSiblings.Add(row);
    }

    var parentRowSiblingsCount = resultSiblings.Count;

    for (var i = 0; i < parentRowSiblingsCount; ++i)
    {
      var rowColumns = resultSiblings[i].SelectNodes("td");

      var colValueName = rowColumns[0].InnerText;
      var colValueType = rowColumns[1].InnerText;

      if (string.IsNullOrEmpty(colValueName))
        continue;

      if (colValueName.IndexOf(ObjectArrayParamMarker, StringComparison.OrdinalIgnoreCase) != -1)
      {
        var oldColValueName = colValueName;
        colValueName = colValueName.Replace(ObjectArrayParamMarker, string.Empty);
        var rowLevel = (oldColValueName.Length - colValueName.Length) / ObjectArrayParamMarker.Length;

        if (rowLevel < level)
          break;
      }

      var isArray = colValueType.StartsWith(ArrayTypeMarker);
      var typeName = isArray ? colValueType[ArrayTypeMarker.Length..] : colValueType;

      var propName = colValueName;

      var prop = new ResponseParameter
      {
        Type = isArray ? "array" : typeName,
        ArrayType = isArray ? typeName : null,
        Description = rowColumns[2].ToMarkdown()
      };

      if (propName.Contains("timestamp"))
        prop.Type = "long";
      else if (prop.Description.Contains("milliseconds since the UNIX epoch"))
        prop.Type = "long";

      prop.Deprecated = GetResponseParamIsDeprecated(prop);
      prop.Optional = GetResponseParamIsOptional(prop);

      if (typeName.Equals("object"))
      {
        var rowsConsumed = ParseComplexType(resultSiblings[i], level + 1, prop);
        i += rowsConsumed;
      }

      props.Add(propName, prop);
    }

    return props;
  }

  private static int ParseComplexType(HtmlNode siblingRow, int level, ResponseParameter param)
  {
    var siblingRowSiblings = new List<HtmlNode>();

    for (var row = siblingRow.NextSibling; row is not null; row = row.NextSibling)
    {
      if (row.Name != "tr")
        continue;

      siblingRowSiblings.Add(row);
    }

    var parentRowSiblingsCount = siblingRowSiblings.Count;

    var props = new ResponseParameterCollection(parentRowSiblingsCount);

    var i = 0;

    for (; i < parentRowSiblingsCount; ++i)
    {
      var rowColumns = siblingRowSiblings[i].SelectNodes("td").ToArray();

      var colValueName = rowColumns[0].InnerText;
      var colValueType = rowColumns[1].InnerText;

      if (colValueName.IndexOf(ObjectArrayParamMarker, StringComparison.OrdinalIgnoreCase) != -1)
      {
        var oldColValueName = colValueName;
        colValueName = colValueName.Replace(ObjectArrayParamMarker, string.Empty);
        var rowLevel = (oldColValueName.Length - colValueName.Length) / ObjectArrayParamMarker.Length;

        if (rowLevel < level)
          break;
      }

      var isArray = colValueType.StartsWith(ArrayTypeMarker);
      var typeName = isArray ? colValueType[ArrayTypeMarker.Length..] : colValueType;

      var propName = colValueName;

      var prop = new ResponseParameter
      {
        Type = isArray ? "array" : typeName,
        ArrayType = isArray ? typeName : null,
        Description = rowColumns[2].ToMarkdown()
      };

      if (propName.Contains("timestamp"))
        prop.Type = "long";

      prop.Deprecated = GetResponseParamIsDeprecated(prop);
      prop.Optional = GetResponseParamIsOptional(prop);

      if (typeName.Equals("object"))
      {
        var rowsConsumed = ParseComplexType(siblingRowSiblings[i], level + 1, prop);
        i += rowsConsumed;
      }

      props.Add(propName, prop);
    }

    param.ObjectParams = props;
    return i;
  }

  private static (HtmlNode? resultPropertyRow, HtmlNode[]? resultPropertyRowColumns) FindRowWithName(IEnumerable<HtmlNode> tableRows, string name, HtmlNode? targetRow, HtmlNode[]? targetRowColumns)
  {
    foreach (var row in tableRows)
    {
      var rowColumns = row.SelectNodes("td");

      var colValueName = rowColumns[0].InnerText;

      if (string.IsNullOrWhiteSpace(colValueName) || !colValueName.Equals(name))
        continue;

      targetRow = row;
      targetRowColumns = rowColumns.ToArray();
      break;
    }

    return (targetRow, targetRowColumns);
  }

  private static void TryRepairTableIfNeeded(ref HtmlNode? resultPropertyRow, ref HtmlNode[]? resultPropertyRowColumns)
  {
    if (resultPropertyRow is null || resultPropertyRowColumns is null)
      return;

    if (resultPropertyRowColumns[1].InnerText.Equals("amount"))
    {
      var nameColumn = resultPropertyRow.OwnerDocument.CreateElement("td");
      var typeColumn = resultPropertyRow.OwnerDocument.CreateElement("td");
      var descriptionColumn = resultPropertyRow.OwnerDocument.CreateElement("td");

      nameColumn.InnerHtml = "result";
      typeColumn.InnerHtml = "array of object";

      var resultRow = resultPropertyRow.OwnerDocument.CreateElement("tr");
      resultRow.AppendChild(nameColumn);
      resultRow.AppendChild(typeColumn);
      resultRow.AppendChild(descriptionColumn);

      resultPropertyRow.ParentNode.InsertBefore(resultRow, resultPropertyRow);

      resultPropertyRowColumns[0].InnerHtml = resultPropertyRowColumns[1].InnerHtml;
      resultPropertyRowColumns[1].InnerHtml = resultPropertyRowColumns[2].InnerHtml;
      resultPropertyRowColumns[2].InnerHtml = "";

      resultPropertyRow = resultRow;
      resultPropertyRowColumns = resultRow.SelectNodes("td").ToArray();
    }
  }

  private static bool GetResponseParamIsDeprecated(ResponseParameter prop)
  {
    Match m;
    var idxPattern = 0;

    do
    {
      m = DeprecatedParamPattern[idxPattern++].Match(prop.Description);
    } while (!m.Success && idxPattern < DeprecatedParamPattern.Length);

    return m.Success;
  }
  private static bool GetResponseParamIsOptional(ResponseParameter prop)
  {
    Match m;
    var idxPattern = 0;

    do
    {
      m = OptionalParamPattern[idxPattern++].Match(prop.Description);
    } while (!m.Success && idxPattern < OptionalParamPattern.Length);

    return m.Success;
  }
}
