namespace DeriSock.DevTools.ApiDoc.Model;

public interface IDocumentationNode
{
  IDocumentationNode? Parent { get; }
  string Name { get; }
}
