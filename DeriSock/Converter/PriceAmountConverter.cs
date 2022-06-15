namespace DeriSock.Converter;

using System;

using DeriSock.Model.Base;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///   Converts an array of [price, amount]
/// </summary>
public class PriceAmountArrayConverter : JsonConverter<PriceAmount>
{
  /// <inheritdoc />
  public override void WriteJson(JsonWriter writer, PriceAmount value, JsonSerializer serializer)
  {
    writer.WriteStartArray();
    writer.WriteValue(value.Price);
    writer.WriteValue(value.Amount);
    writer.WriteEndArray();
  }

  /// <inheritdoc />
  public override PriceAmount ReadJson
  (
    JsonReader reader, Type objectType,
    PriceAmount existingValue, bool hasExistingValue,
    JsonSerializer serializer
  )
  {
    var arr = JArray.Load(reader);

    return new PriceAmount
    {
      Price = arr[0].Value<decimal>(),
      Amount = arr[1].Value<decimal>()
    };
  }
}
