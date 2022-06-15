namespace DeriSock.DevTools.ApiDoc.Analysis;

using System.Collections.Generic;

using DeriSock.DevTools.ApiDoc.Model;

public struct AnalysisEntry
{
  public AnalysisType Type { get; set; }
  public string Message { get; set; }
  public LinkedList<IDocumentationNode> ItemTree { get; set; }
}
