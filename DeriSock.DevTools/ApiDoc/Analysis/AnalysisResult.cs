namespace DeriSock.DevTools.ApiDoc.Analysis;

using System.Collections.Generic;
using System.Linq;

public class AnalysisResult
{
  public IList<AnalysisEntry> Entries { get; set; } = new List<AnalysisEntry>();

  public bool HasType(AnalysisType type)
  {
    return Entries.Any(e => e.Type == type);
  }

  public IEnumerable<AnalysisEntry> GetEntries(AnalysisType type)
  {
    return Entries.Where(e => e.Type == type);
  }

  public IEnumerable<IGrouping<AnalysisType, AnalysisEntry>> GetEntriesGroupedByType()
  {
    return Entries.GroupBy(e => e.Type);
  }
}
