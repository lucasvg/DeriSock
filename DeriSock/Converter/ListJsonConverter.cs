﻿namespace DeriSock.Converter;

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

/// <inheritdoc />
public class ListJsonConverter<T> : IJsonConverter<List<T>>
{
  /// <inheritdoc />
  public List<T> Convert(JToken value)
  {
    var list = (JArray)value;
    var result = new List<T>(list.Count);
    foreach (var item in list)
    {
      result.Add(item.ToObject<T>());
    }

    return result;
  }
}
