namespace DeriSock.DevTools.ApiDoc;

using System.Collections.Generic;

using DeriSock.DevTools.ApiDoc.Model;

public record ParameterVisitItem
{
  public KeyValuePair<string, Parameter> Current;
  public ParameterVisitItem? Parent;
}
