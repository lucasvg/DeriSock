namespace DeriSock.DevTools;

using System.Linq;
using System.Text;

using DeriSock.DevTools.ApiDoc.Model;

public static class Extensions
{
  public static bool IsRegexPattern(this string value)
  {
    return value.Any(c => c is '^' or '$' or '*' or '?' or '+' or '[' or ']' or '(' or ')' or '|');
  }

  public static string GetPath(this IDocumentationNode value)
  {
    var sb = new StringBuilder(100);

    var first = true;
    for (IDocumentationNode? item = value; item != null; item = item.Parent) {
      if (!first)
        sb.Insert(0, ".");

      first = false;
      sb.Insert(0, item.Name);
    }
    return sb.ToString();
  }
}
