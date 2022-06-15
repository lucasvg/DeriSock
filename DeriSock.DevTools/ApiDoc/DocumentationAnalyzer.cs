namespace DeriSock.DevTools.ApiDoc;

using System;
using System.Collections.Generic;

using DeriSock.DevTools.ApiDoc.Analysis;
using DeriSock.DevTools.ApiDoc.Model;

public class DocumentationAnalyzer
{
  private readonly Documentation _documentation;
  private readonly AnalysisResult _result = new();

  public DocumentationAnalyzer(Documentation documentation)
  {
    _documentation = documentation;
  }

  public AnalysisResult Analyze()
  {
    CheckForMissingManagedTypeAndObjectParams();

    return _result;
  }

  private void CheckForMissingManagedTypeAndObjectParams()
  {
    var fnAddEntry = new Action<AnalysisType, string, IDocumentationNode>((type, message, item) =>
    {
      var entry = new AnalysisEntry
      {
        Type = type,
        Message = message
      };

      var ll = new LinkedList<IDocumentationNode>();

      for (var curItem = item; curItem != null; curItem = curItem.Parent)
        ll.AddFirst(item);

      entry.ItemTree = ll;
      _result.Entries.Add(entry);
    });

    // Check all the methods
    foreach (var (_, method) in _documentation.Methods) {
      // Check the methods parameters
      if (method.Params is not null)
        foreach (var (_, param) in method.Params) {
          if (!param.IsComplex)
            continue;

          if (param.ObjectArrayParams is null) {
            if (param.IsArray) {
              // Add error. Complex Parameter without ObjectArrayParams
              fnAddEntry(AnalysisType.MissingObjectArrayParams, "Missing ObjectArrayParams on array of objects type", param);
            }

            continue;
          }

          param.ObjectArrayParams.Visit(param, (_, item) =>
          {
            var parameter = (Parameter)item;

            if (!parameter.IsComplex)
              return;

            if (parameter.IsArray && parameter.ObjectArrayParams is null) {
              // Add error. Complex Array Parameter without ObjectArrayParams
              fnAddEntry(AnalysisType.MissingObjectArrayParams, "Missing ObjectArrayParams on array of objects type", item);
              return;
            }

            if (parameter.ManagedType is not null)
              return;

            // Add error. Complex Parameter without ManagedType
            fnAddEntry(AnalysisType.MissingManagedType, "Missing managed type definition on complex type", item);
          });
        }

      // Check the methods response
      if (method.Response is not null && method.Response.IsComplex) {
        if (method.Response.ObjectParams is null) {
          // Add error. Complex Response without ObjectParams
          fnAddEntry(AnalysisType.MissingObjectParams, "Missing ObjectParams on complex type", method.Response);
          continue;
        }

        if (method.Response.ManagedType is null) {
          // Add error. Complex Response without ManagedType
          fnAddEntry(AnalysisType.MissingManagedType, "Missing managed type definition on complex type", method.Response);
        }

        method.Response.ObjectParams.Visit(method.Response, (_, item) =>
        {
          var responseParam = (ResponseParameter)item;

          if (!responseParam.IsComplex)
            return;

          if (responseParam.ObjectParams is null) {
            // Add error. Complex Response Parameter without ObjectParams
            fnAddEntry(AnalysisType.MissingObjectParams, "Missing ObjectParams on complex type", item);
            return;
          }

          if (responseParam.ManagedType is not null)
            return;

          // Add error. Complex Response without ManagedType
          fnAddEntry(AnalysisType.MissingManagedType, "Missing managed type definition on complex type", item);
        });
      }
    }

    // Check all the subscriptions
    foreach (var (_, subscription) in _documentation.Subscriptions) {
      // Check the subscriptions parameters
      if (subscription.Params is not null)
        foreach (var (_, param) in subscription.Params) {
          if (!param.IsComplex)
            continue;

          if (param.ObjectArrayParams is null) {
            if (param.IsArray) {
              // Add error. Complex Parameter without ObjectArrayParams
              fnAddEntry(AnalysisType.MissingObjectArrayParams, "Missing ObjectArrayParams on array of objects type", param);
            }

            continue;
          }

          param.ObjectArrayParams.Visit(param, (_, item) =>
          {
            var parameter = (Parameter)item;

            if (!parameter.IsComplex)
              return;

            if (parameter.IsArray && parameter.ObjectArrayParams is null) {
              // Add error. Complex Array Parameter without ObjectArrayParams
              fnAddEntry(AnalysisType.MissingObjectArrayParams, "Missing ObjectArrayParams on array of objects type", item);
              return;
            }

            if (parameter.ManagedType is not null)
              return;

            // Add error. Complex Parameter without ManagedType
            fnAddEntry(AnalysisType.MissingManagedType, "Missing managed type definition on complex type", item);
          });
        }

      // Check the subscriptions response
      if (subscription.Response is not null && subscription.Response.IsComplex) {
        if (subscription.Response.ObjectParams is null) {
          // Add error. Complex Response without ObjectParams
          fnAddEntry(AnalysisType.MissingObjectParams, "Missing ObjectParams on complex type", subscription.Response);
          continue;
        }

        if (subscription.Response.ManagedType is null) {
          // Add error. Complex Response without ManagedType
          fnAddEntry(AnalysisType.MissingManagedType, "Missing managed type definition on complex type", subscription.Response);
        }

        subscription.Response.ObjectParams.Visit(subscription.Response, (_, item) =>
        {
          var responseParam = (ResponseParameter)item;

          if (!responseParam.IsComplex)
            return;

          if (responseParam.ObjectParams is null) {
            // Add error. Complex Response Parameter without ObjectParams
            fnAddEntry(AnalysisType.MissingObjectParams, "Missing ObjectParams on complex type", item);
            return;
          }

          if (responseParam.ManagedType is not null)
            return;

          // Add error. Complex Response without ManagedType
          fnAddEntry(AnalysisType.MissingManagedType, "Missing managed type definition on complex type", item);
        });
      }
    }
  }
}
