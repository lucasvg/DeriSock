namespace DeriSock.DevTools.ApiDoc.Model;

using System.Text.Json.Serialization;

using JetBrains.Annotations;

public class Documentation : IDocumentationNode
{
  IDocumentationNode? IDocumentationNode.Parent => null;

  string IDocumentationNode.Name => "ApiDoc";

  [JsonPropertyName("$schema")]
  public string Schema { get; set; } = "https://raw.githubusercontent.com/psollberger/DeriSock/main/DeriSock.DevTools/ApiSchema/deribit.api.schema.json";

  [JsonPropertyName("version")]
  public string Version { get; set; }

  [JsonPropertyName("endpoints")]
  public Endpoints Endpoints { get; set; } = new();

  [JsonPropertyName("methods")]
  public MethodCollection Methods { get; set; }

  [JsonPropertyName("subscriptions")]
  public SubscriptionCollection Subscriptions { get; set; }

  [UsedImplicitly]
  public Documentation()
  {
    Version = string.Empty;
    Methods = new MethodCollection();
    Subscriptions = new SubscriptionCollection();
  }

  public Documentation(string version, MethodCollection methods, SubscriptionCollection subscriptions)
  {
    Version = version;
    Methods = methods;
    Subscriptions = subscriptions;
  }

  public override string ToString()
  {
    return this.GetPath();
  }
}
