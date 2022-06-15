namespace DeriSock.DevTools.ApiDoc;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

using DeriSock.DevTools.ApiDoc.Model;
using DeriSock.DevTools.ApiDoc.Overrides;

using HtmlAgilityPack;

public class DocumentationBuilder
{
  public string Version { get; private set; } = "0.0.0";
  public MethodCollection Methods { get; } = new(130);
  public SubscriptionCollection Subscriptions { get; } = new(40);
  private DocumentationOverride? _apiOverrides;
  private readonly HtmlDocument _htmlDocument;

  public static async Task<Documentation> FromUrlAsync(string apiDocUrl)
  {
    var parser = new DocumentationBuilder(await new HtmlWeb().LoadFromWebAsync(apiDocUrl));

    parser.ParseVersion();
    parser.ParseMethods();
    parser.ParseSubscriptions();

    return new Documentation(parser.Version, parser.Methods, parser.Subscriptions);
  }

  public static async Task<Documentation?> FromFileAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException(filePath);

    var fileContent = await File.ReadAllTextAsync(filePath);
    var apiDoc = JsonSerializer.Deserialize<Documentation>(fileContent);

    if (apiDoc is null)
      return null;

    foreach (var (_, method) in apiDoc.Methods) {
      method.Parent = apiDoc;

      if (method.Params is not null)
        foreach (var (_, param) in method.Params) {
          param.Parent = method;

          param.ObjectArrayParams?.Visit(param, (parent, item) =>
          {
            ((Parameter)item).Parent = parent;
          });
        }

      if (method.Response is not null) {
        method.Response.Parent = method;

        method.Response.ObjectParams?.Visit(method.Response, (parent, item) =>
        {
          switch (item) {
            case Response resp:
              resp.Parent = parent;
              break;
            case ResponseParameter respParam:
              respParam.Parent = parent;
              break;
          }
        });
      }
    }

    foreach (var (_, subscription) in apiDoc.Subscriptions) {
      subscription.Parent = apiDoc;

      if (subscription.Params is not null)
        foreach (var (_, param) in subscription.Params) {
          param.Parent = subscription;

          param.ObjectArrayParams?.Visit(param, (parent, item) =>
          {
            ((Parameter)item).Parent = parent;
          });
        }

      if (subscription.Response is not null) {
        subscription.Response.Parent = subscription;

        subscription.Response.ObjectParams?.Visit(subscription.Response, (parent, item) =>
        {
          switch (item) {
            case Response resp:
              resp.Parent = parent;
              break;
            case ResponseParameter respParam:
              respParam.Parent = parent;
              break;
          }
        });
      }
    }

    return apiDoc;
  }

  public static Task ApplyOverrides(Documentation apiDoc, string apiDocFilePath)
  {
    var parser = new DocumentationBuilder(apiDoc);

    parser.ApplyOverrides(apiDocFilePath);

    apiDoc.Version = parser.Version;
    apiDoc.Methods = parser.Methods;
    apiDoc.Subscriptions = parser.Subscriptions;

    return Task.CompletedTask;
  }

  private DocumentationBuilder(HtmlDocument htmlDoc)
  {
    _htmlDocument = htmlDoc;
  }

  private DocumentationBuilder(Documentation apiDoc)
  {
    _htmlDocument = new HtmlDocument();

    Version = apiDoc.Version;
    Methods = apiDoc.Methods;
    Subscriptions = apiDoc.Subscriptions;
  }

  private void ParseVersion()
  {
    var versionNode = FindVersionTitle(_htmlDocument);

    if (versionNode is null)
      return;

    Version = versionNode.InnerText["Deribit API v".Length..];
  }

  private void ParseMethods()
  {
    foreach (var categoryTitleNode in FindAllCategoryTitles(_htmlDocument)) {
      foreach (var methodTitleNode in FindAllCategoryMethodTitles(categoryTitleNode)) {
        var (key, value) = ParseMethod(categoryTitleNode, methodTitleNode);
        Methods.Add(key, value);
      }
    }
  }

  private void ParseSubscriptions()
  {
    foreach (var subscriptionTitleNode in FindAllSubscriptionTitles(_htmlDocument)) {
      var (key, value) = ParseSubscription(subscriptionTitleNode);
      Subscriptions.Add(key, value);
    }
  }

  private static KeyValuePair<string, Method> ParseMethod(HtmlNode categoryNode, HtmlNode titleNode)
  {
    // <h1 id="methods">
    // ...
    // ...
    // ...
    // <h2 id="public-method">/public/method</h2>
    // <pre> elements with classes: highlight <language> tab-<language>
    // also (maybe) a blockquote element
    // [0..n] <p> elements with method description
    // [0..1] <aside> When it's a private method
    // <h3 id="parameters[-n]">
    // <table> | <p> > <em>This method takes no parameters</em>
    // <h3 id="response[-n]>
    // <table>
    // ...
    // ... repeat from h2
    // ...

    var categoryName = categoryNode.InnerText;
    var methodName = titleNode.InnerText[1..];

    var result = new Method
    {
      Category = categoryName
    };

    foreach (var descNode in GetAllDescriptionNodes(titleNode)) {
      if (descNode.Name == "li")
        result.Description += "- ";

      result.Description += descNode.ToMarkdown() + "\n";
    }

    result.Description = HttpUtility.HtmlDecode(result.Description.TrimEnd());

    result.Deprecated = GetSectionDeprecated(titleNode) ? true : null;

    var hasParameters = true;
    var hasResponse = true;
    var paramsTableBody = titleNode.NextSibling;

    while (paramsTableBody.Name != "table") {
      if (paramsTableBody.Name == "p" && paramsTableBody.InnerText == "This method takes no parameters") {
        hasParameters = false;
        break;
      }

      paramsTableBody = paramsTableBody.NextSibling;
    }

    var responseTableBody = paramsTableBody.NextSibling;

    while (responseTableBody.Name != "table") {
      if (responseTableBody.Name == "p" && responseTableBody.InnerText == "This method has no response body") {
        hasResponse = false;
        break;
      }

      responseTableBody = responseTableBody.NextSibling;
    }

    if (hasParameters)
      result.Params = ParameterTableParser.Parse(paramsTableBody);

    if (hasResponse)
      result.Response = ResponseTableParser.Parse(responseTableBody, "result");

    return KeyValuePair.Create(methodName, result);
  }

  private static KeyValuePair<string, Subscription> ParseSubscription(HtmlNode titleNode)
  {
    // <h1 id="subscriptions">
    // ...
    // ...
    // ...
    // <h2 id="subscription-name">subscription-name-with-variables-in-curly-braces</h2>
    // <blockquote> telling subscriptions only with websockets
    // <pre> elements with classes: highlight <language> tab-<language>
    // also (maybe) a blockquote element
    // [0..n] <p> elements with method description
    // <h3 id="channel-parameters[-n]">
    // <table> | <p> > <em>This channel takes no parameters</em>
    // <h3 id="response[-n]>
    // <table>
    // ...
    // ... repeat from h2
    // ...


    var channelName = titleNode.InnerText;

    var result = new Subscription();

    foreach (var descNode in GetAllDescriptionNodes(titleNode)) {
      if (descNode.Name == "li")
        result.Description += "- ";

      result.Description += descNode.ToMarkdown() + "\n";
    }

    result.Description = HttpUtility.HtmlDecode(result.Description.TrimEnd());

    var hasParameters = true;
    var paramsTableBody = titleNode.NextSibling;

    while (paramsTableBody.Name != "table") {
      if (paramsTableBody.Name == "p" && paramsTableBody.InnerText is "This channel takes no parameters" or "This method takes no parameters") {
        hasParameters = false;
        break;
      }

      paramsTableBody = paramsTableBody.NextSibling;
    }

    var responseTableBody = paramsTableBody.NextSibling;

    while (responseTableBody.Name != "table")
      responseTableBody = responseTableBody.NextSibling;

    if (hasParameters)
      result.Params = ParameterTableParser.Parse(paramsTableBody);

    result.Response = ResponseTableParser.Parse(responseTableBody, "data");

    return KeyValuePair.Create(channelName, result);
  }

  private void ApplyOverrides(string apiDocFilePath)
  {
    var directoryPath = Path.GetDirectoryName(Path.GetFullPath(apiDocFilePath));

    if (string.IsNullOrEmpty(directoryPath))
      return;

    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(apiDocFilePath);

    var overrideFileNamePattern = new Regex(@$"^{Regex.Escape(fileNameWithoutExtension)}(\.(?!\.)[0-9]+\.(?!\.)\w+)?\.overrides\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

    var jsonFilesInFolder = Directory.GetFiles(directoryPath, "*.json").Select(filePath => Path.GetFileName(filePath)!);

    var overrideFileNames = jsonFilesInFolder
      .Where(fileName => overrideFileNamePattern.IsMatch(fileName))
      .Select(fileName => Path.Combine(directoryPath, fileName)).ToArray();

    foreach (var overrideFileName in overrideFileNames) {
      _apiOverrides = JsonSerializer.Deserialize<DocumentationOverride>(File.ReadAllText(overrideFileName));

      if (_apiOverrides?.Methods is not null)
        foreach (var (methodKey, methodInfo) in _apiOverrides.Methods)
          ApplyMethodOverrides(methodKey, KeyValuePair.Create(methodKey, methodInfo));

      if (_apiOverrides?.Subscriptions is not null)
        foreach (var (subscriptionKey, subscriptionInfo) in _apiOverrides.Subscriptions)
          ApplySubscriptionOverrides(subscriptionKey, KeyValuePair.Create(subscriptionKey, subscriptionInfo));
    }

    _apiOverrides = null;
  }

  private void ApplyMethodOverrides(string methodKey, KeyValuePair<string, MethodOverride> methodOverrideEntry)
  {
    var methodsToOverride = methodKey.IsRegexPattern() ?
                              Methods.Where(m => Regex.IsMatch(m.Key, methodKey)).ToArray() :
                              Methods.Where(m => m.Key.Equals(methodKey)).ToArray();

    if (methodsToOverride.Length < 1)
      return;

    var methodOverride = methodOverrideEntry.Value;

    foreach (var (_, method) in methodsToOverride) {
      if (methodOverride.Description is not null)
        method.Description = methodOverride.Description;

      if (methodOverride.Deprecated is not null)
        method.Deprecated = methodOverride.Deprecated;

      if (methodOverride.Params is not null && method.Params is not null)
        foreach (var (paramKey, overrideParam) in methodOverride.Params) {
          var methodParamsToOverride = paramKey.IsRegexPattern() ?
                                         method.Params.Where(p => Regex.IsMatch(p.Key, paramKey)).ToArray() :
                                         method.Params.Where(p => p.Key.Equals(paramKey)).ToArray();

          if (methodParamsToOverride.Length < 1)
            continue;

          foreach (var methodParam in methodParamsToOverride)
            ApplyParameterOverrides(methodParam.Value, overrideParam);
        }

      if (methodOverride.Response is not null && method.Response is not null)
        ApplyResponseOverrides(method.Response, methodOverride.Response);

      if (methodOverride.Response?.ObjectParams is not null && method.Response?.ObjectParams is not null)
        foreach (var (paramKey, overrideResponseParam) in methodOverride.Response.ObjectParams) {
          var searchKey = paramKey;

          if (overrideResponseParam.Name is not null) {
            method.Response.ObjectParams.RenameKey(paramKey, overrideResponseParam.Name);
            searchKey = overrideResponseParam.Name;
          }

          var methodResponseParamsToOverride = searchKey.IsRegexPattern() ?
                                                 method.Response.ObjectParams.Where(op => Regex.IsMatch(op.Key, searchKey)).ToArray() :
                                                 method.Response.ObjectParams.Where(op => op.Key.Equals(searchKey)).ToArray();

          if (methodResponseParamsToOverride.Length < 1)
            continue;

          foreach (var methodResponseParam in methodResponseParamsToOverride)
            ApplyResponseParameterOverrides(methodResponseParam.Value, overrideResponseParam);
        }
    }
  }

  private void ApplySubscriptionOverrides(string subscriptionKey, KeyValuePair<string, SubscriptionOverride> subscriptionOverrideEntry)
  {
    var subscriptionsToOverride = subscriptionKey.IsRegexPattern() ?
                                    Subscriptions.Where(s => Regex.IsMatch(s.Key, subscriptionKey)).ToArray() :
                                    Subscriptions.Where(s => s.Key.Equals(subscriptionKey)).ToArray();

    if (subscriptionsToOverride.Length < 1)
      return;

    var subscriptionOverride = subscriptionOverrideEntry.Value;

    foreach (var (_, subscription) in subscriptionsToOverride) {
      if (subscriptionOverride.Description is not null)
        subscription.Description = subscriptionOverride.Description;

      if (subscriptionOverride.Params is not null && subscription.Params is not null)
        foreach (var (paramKey, overrideParam) in subscriptionOverride.Params) {
          var subscriptionParamsToOverride = paramKey.IsRegexPattern() ?
                                               subscription.Params.Where(p => Regex.IsMatch(p.Key, paramKey)).ToArray() :
                                               subscription.Params.Where(p => p.Key.Equals(paramKey)).ToArray();

          if (subscriptionParamsToOverride.Length < 1)
            continue;

          foreach (var subscriptionParam in subscriptionParamsToOverride)
            ApplyParameterOverrides(subscriptionParam.Value, overrideParam);
        }

      if (subscriptionOverride.Response is not null && subscription.Response is not null)
        ApplyResponseOverrides(subscription.Response, subscriptionOverride.Response);

      if (subscriptionOverride.Response?.ObjectParams is not null && subscription.Response?.ObjectParams is not null)
        foreach (var (paramKey, overrideResponseParam) in subscriptionOverride.Response.ObjectParams) {
          var searchKey = paramKey;

          if (overrideResponseParam.Name is not null) {
            subscription.Response.ObjectParams.RenameKey(paramKey, overrideResponseParam.Name);
            searchKey = overrideResponseParam.Name;
          }

          var methodResponseParamsToOverride = searchKey.IsRegexPattern() ?
                                                 subscription.Response.ObjectParams.Where(op => Regex.IsMatch(op.Key, searchKey)).ToArray() :
                                                 subscription.Response.ObjectParams.Where(op => op.Key.Equals(searchKey)).ToArray();

          if (methodResponseParamsToOverride.Length < 1)
            continue;

          foreach (var methodResponseParam in methodResponseParamsToOverride)
            ApplyResponseParameterOverrides(methodResponseParam.Value, overrideResponseParam);
        }
    }
  }

  private void ApplyParameterOverrides(Parameter methodParam, ParameterOverride overrideParam)
  {
    if (overrideParam.Required is not null)
      methodParam.Required = overrideParam.Required.Value;

    if (overrideParam.Type is not null)
      methodParam.Type = overrideParam.Type;

    if (overrideParam.ArrayType is not null)
      methodParam.ArrayType = overrideParam.ArrayType;

    if (overrideParam.Default is not null)
      methodParam.Default = overrideParam.Default;

    if (overrideParam.Enum is not null)
      methodParam.Enum = overrideParam.Enum;

    if (overrideParam.MaxLength is not null)
      methodParam.MaxLength = overrideParam.MaxLength.Value;

    if (overrideParam.Description is not null)
      methodParam.Description = overrideParam.Description;

    if (overrideParam.ManagedType is not null)
      methodParam.ManagedType = overrideParam.ManagedType;

    if (overrideParam.ObjectArrayParams is not null)
      foreach (var (_, overrideParamObjectArray) in overrideParam.ObjectArrayParams) {
        var searchKey = overrideParamObjectArray.Name!;

        var methodParamObjectArraysToOverride = searchKey.IsRegexPattern() ?
                                                  methodParam.ObjectArrayParams!.Where(p => Regex.IsMatch(p.Key, searchKey)).ToArray() :
                                                  methodParam.ObjectArrayParams!.Where(p => p.Key.Equals(searchKey)).ToArray();

        if (methodParamObjectArraysToOverride.Length < 1)
          continue;

        foreach (var methodParamObjectArray in methodParamObjectArraysToOverride)
          ApplyParameterOverrides(methodParamObjectArray.Value, overrideParam);
      }
  }

  private void ApplyResponseOverrides(Response methodResponse, ResponseOverride overrideResponse)
  {
    if (overrideResponse.ManagedType is not null)
      methodResponse.ManagedType = overrideResponse.ManagedType;

    if (overrideResponse.Type is not null)
      methodResponse.Type = overrideResponse.Type;

    if (overrideResponse.ArrayType is not null)
      methodResponse.ArrayType = overrideResponse.ArrayType;

    if (overrideResponse.Description is not null)
      methodResponse.Description = overrideResponse.Description;
  }

  private void ApplyResponseParameterOverrides(ResponseParameter methodResponseParam, ResponseParameterOverride overrideResponseParam)
  {
    if (overrideResponseParam.ManagedType is not null)
      methodResponseParam.ManagedType = overrideResponseParam.ManagedType;

    if (overrideResponseParam.Type is not null)
      methodResponseParam.Type = overrideResponseParam.Type;

    if (overrideResponseParam.ArrayType is not null)
      methodResponseParam.ArrayType = overrideResponseParam.ArrayType;

    if (overrideResponseParam.Description is not null)
      methodResponseParam.Description = overrideResponseParam.Description;

    if (overrideResponseParam.Deprecated.HasValue)
      methodResponseParam.Deprecated = overrideResponseParam.Deprecated.Value;

    if (overrideResponseParam.Optional.HasValue)
      methodResponseParam.Optional = overrideResponseParam.Optional.Value;

    if (overrideResponseParam.ObjectParams is not null && methodResponseParam.ObjectParams is not null)
      foreach (var (paramKey, overrideResponseParamObjectParam) in overrideResponseParam.ObjectParams) {
        var searchKey = paramKey;

        if (overrideResponseParamObjectParam.Name is not null) {
          methodResponseParam.ObjectParams.RenameKey(paramKey, overrideResponseParamObjectParam.Name);
          searchKey = overrideResponseParamObjectParam.Name;
        }

        var methodResponseParamObjectParamsToOverride = searchKey.IsRegexPattern() ?
                                               methodResponseParam.ObjectParams.Where(op => Regex.IsMatch(op.Key, searchKey)).ToArray() :
                                               methodResponseParam.ObjectParams.Where(op => op.Key.Equals(searchKey)).ToArray();

        if (methodResponseParamObjectParamsToOverride.Length < 1)
          continue;

        foreach (var methodResponseParamObjectParam in methodResponseParamObjectParamsToOverride)
          ApplyResponseParameterOverrides(methodResponseParamObjectParam.Value, overrideResponseParamObjectParam);
      }
  }

  private static HtmlNode? FindVersionTitle(HtmlDocument htmlDoc)
  {
    var overviewTitleNode = htmlDoc.GetElementbyId("overview");
    return overviewTitleNode?.PreviousSibling;
  }

  private static IEnumerable<HtmlNode> FindAllCategoryTitles(HtmlDocument htmlDoc)
  {
    var methodsTitleNode = htmlDoc.GetElementbyId("methods");

    if (methodsTitleNode == null)
      yield break;

    var curNode = methodsTitleNode;

    while (true) {
      curNode = curNode.NextSibling;

      if (curNode == null)
        break;

      if (curNode.Id == "subscriptions")
        yield break;

      if (curNode.Name != "h1")
        continue;

      yield return curNode;
    }
  }

  private static IEnumerable<HtmlNode> FindAllCategoryMethodTitles(HtmlNode categoryTitleNode)
  {
    var curNode = categoryTitleNode;

    while (true) {
      curNode = curNode.NextSibling;

      if (curNode == null)
        break;

      if (curNode.Name == "h1")
        yield break;

      if (curNode.Name != "h2")
        continue;

      yield return curNode;
    }
  }

  private static IEnumerable<HtmlNode> GetAllDescriptionNodes(HtmlNode titleNode)
  {
    var curNode = titleNode.NextSibling;

    while (curNode.Name != "p")
      curNode = curNode.NextSibling;

    while (curNode.Name != "h3") {
      switch (curNode.Name) {
        case "#text":
          // do nothing with those nodes
          break;
        case "aside":
          // A notice.
          // With class multi-notice: This is a private method; it can only be used after authentication.
          // With class warning: This method is deprecated and will be removed in the future.
          break;
        case "ul":
          foreach (var liNode in curNode.ChildNodes) {
            if (liNode.Name == "#text")
              continue;

            yield return liNode;
          }

          break;
        default:
          if (curNode.Name == "p" && curNode.InnerText.Equals("Try in API console"))
            break;

          yield return curNode;

          break;
      }

      curNode = curNode.NextSibling;
    }
  }

  private static bool GetSectionDeprecated(HtmlNode titleNode)
  {
    var curNode = titleNode.NextSibling;

    while (curNode.Name != "p")
      curNode = curNode.NextSibling;

    while (curNode.Name != "h3") {
      switch (curNode.Name) {
        case "aside":
          // A notice.
          // With class multi-notice: This is a private method; it can only be used after authentication.
          // With class warning: This method is deprecated and will be removed in the future.
          if (curNode.HasClass("warning") && curNode.InnerText.Contains("is deprecated and will be removed", StringComparison.OrdinalIgnoreCase))
            return true;

          break;
      }

      curNode = curNode.NextSibling;
    }

    return false;
  }

  private static IEnumerable<HtmlNode> FindAllSubscriptionTitles(HtmlDocument htmlDoc)
  {
    var subscriptionsTitleNode = htmlDoc.GetElementbyId("subscriptions");

    if (subscriptionsTitleNode == null)
      yield break;

    var curNode = subscriptionsTitleNode;

    while (true) {
      curNode = curNode.NextSibling;

      if (curNode == null)
        break;

      if (curNode.Id == "rpc-error-codes")
        yield break;

      if (curNode.Name != "h2")
        continue;

      yield return curNode;
    }
  }
}
