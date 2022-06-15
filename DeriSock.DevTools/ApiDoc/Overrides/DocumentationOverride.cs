namespace DeriSock.DevTools.ApiDoc.Overrides;

using System.Text.Json.Serialization;

public class DocumentationOverride
{
  [JsonPropertyName("methods")]
  public MethodOverrideCollection? Methods { get; set; }

  [JsonPropertyName("subscriptions")]
  public SubscriptionOverrideCollection? Subscriptions { get; set; }
}
