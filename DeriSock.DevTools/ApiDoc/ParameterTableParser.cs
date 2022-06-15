namespace DeriSock.DevTools.ApiDoc;

using System;
using System.Text.RegularExpressions;

using DeriSock.DevTools.ApiDoc.Model;

using HtmlAgilityPack;

internal static class ParameterTableParser
{
  private const string ObjectArrayTypeMarker = "array of object";
  private const string ObjectArrayTypeName = "objectArray";
  private const string ObjectArrayParamMarker = "&nbsp;&nbsp;&rsaquo;&nbsp;&nbsp;";

  private static readonly Regex[] DefaultValuePattern =
  {
    new(@"default: (?<value>\S+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"default - (?<value>[^)]+).*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"by default (?<value>\S+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"by default (?<value>\S+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"\(default (?<value>\S+)\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled),
    new(@"\. Default (?<value>\S+)<", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled)
  };

  public static ParameterCollection Parse(HtmlNode tableNode)
  {
    var apiMethodParams = new ParameterCollection(10);
    var apiMethodParamParams = new ParameterCollection(10);

    tableNode = tableNode.FirstChild.NextSibling;
    var paramsTableRows = tableNode.SelectNodes("tr");

    Parameter? objectArrayParam = null;

    foreach (var row in paramsTableRows) {
      var rowColumns = row.SelectNodes("td");

      var paramName = rowColumns[0].InnerText;

      var param = new Parameter
      {
        Required = rowColumns[1].InnerText == "true",
        Type = rowColumns[2].InnerText,
        Enum = rowColumns[3].InnerHtml.Split("<br>"),
        Description = rowColumns[4].ToMarkdown()
      };

      var isObjectArrayParam = paramName.StartsWith(ObjectArrayParamMarker, StringComparison.Ordinal);

      if (isObjectArrayParam)
        paramName = paramName.Replace(ObjectArrayParamMarker, string.Empty);

      if (param.Enum.Length == 1 && string.IsNullOrEmpty(param.Enum[0])) {
        param.Enum = null;
      }
      else {
        for (var i = 0; i < param.Enum.Length; i++) {
          param.Enum[i] = param.Enum[i].Replace("<code>", string.Empty).Replace("</code>", string.Empty);
        }
      }

      param.Default = GetDefaultValue(param);

      if (isObjectArrayParam && objectArrayParam is not null) {
        apiMethodParamParams.Add(paramName, param);
      }
      else {
        if (objectArrayParam is not null) {
          objectArrayParam.ObjectArrayParams = apiMethodParamParams;
          objectArrayParam = null;
        }

        if (param.Type.StartsWith(ObjectArrayTypeMarker, StringComparison.OrdinalIgnoreCase)) {
          param.Type = ObjectArrayTypeName;

          apiMethodParamParams.Clear();
          objectArrayParam = param;
        }

        if (paramName.Contains("timestamp"))
          param.Type = "long";

        apiMethodParams.Add(paramName, param);
      }
    }

    if (objectArrayParam is not null) {
      objectArrayParam.ObjectArrayParams = apiMethodParamParams;
    }

    return apiMethodParams;
  }

  private static object? GetDefaultValue(Parameter prop)
  {
    Match m;
    var idxPattern = 0;

    do {
      m = DefaultValuePattern[idxPattern++].Match(prop.Description);
    } while (!m.Success && idxPattern < DefaultValuePattern.Length);

    if (!m.Success)
      return null;

    var value = m.Groups["value"].Value;
    value = value.Replace("`", "");
    value = value.Replace("\"", "");

    if (bool.TrueString.Equals(value, StringComparison.OrdinalIgnoreCase))
      return true;

    if (bool.FalseString.Equals(value, StringComparison.OrdinalIgnoreCase))
      return false;

    if (value.Contains('.') && double.TryParse(value, out var numNumeric))
      return numNumeric;

    if (int.TryParse(value, out var numInteger))
      return numInteger;

    return value;
  }
}
