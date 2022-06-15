namespace DeriSock.DevTools.ApiDoc.Overrides;

using System.Text.Json.Serialization;

public class SubscriptionOverride
{
  [JsonPropertyName("channelName")]
  public string ChannelName { get; set; } = null!;

  [JsonPropertyName("description")]
  public string? Description { get; set; }

  [JsonPropertyName("params")]
  public ParameterOverrideCollection? Params { get; set; }

  [JsonPropertyName("response")]
  public ResponseOverride? Response { get; set; }
}
