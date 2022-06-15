namespace DeriSock.DevTools.ApiDoc.Model;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

public class ResponseParameterCollection : IDictionary<string, ResponseParameter>
{
  private readonly IDictionary<string, ResponseParameter> _dict;

  [UsedImplicitly]
  public ResponseParameterCollection()
  {
    _dict = new Dictionary<string, ResponseParameter>();
  }

  public ResponseParameterCollection(int capacity)
  {
    _dict = new Dictionary<string, ResponseParameter>(capacity);
  }

  public void Visit(IDocumentationNode? parent, Action<IDocumentationNode?, IDocumentationNode> visitor)
  {
    foreach (var kvp in this) {
      visitor(parent, kvp.Value);
      kvp.Value.ObjectParams?.Visit(kvp.Value, visitor);
    }
  }

  public override int GetHashCode()
  {
    var hashCodes = Keys.Select(key => HashCode.Combine(key, this[key])).ToArray();
    return hashCodes.Aggregate(hashCodes.Length, (current, code) => unchecked(current * 314159 + code));
  }

#region Dictionary

  public IEnumerator<KeyValuePair<string, ResponseParameter>> GetEnumerator()
  {
    return _dict.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return ((IEnumerable)_dict).GetEnumerator();
  }

  public void Add(KeyValuePair<string, ResponseParameter> item)
  {
    item.Value.Name = item.Key;
    _dict.Add(item);
  }

  public void Clear()
  {
    _dict.Clear();
  }

  public bool Contains(KeyValuePair<string, ResponseParameter> item)
  {
    return _dict.Contains(item);
  }

  public void CopyTo(KeyValuePair<string, ResponseParameter>[] array, int arrayIndex)
  {
    _dict.CopyTo(array, arrayIndex);
  }

  public bool Remove(KeyValuePair<string, ResponseParameter> item)
  {
    return _dict.Remove(item);
  }

  public int Count => _dict.Count;

  public bool IsReadOnly => _dict.IsReadOnly;

  public void Add(string key, ResponseParameter value)
  {
    value.Name = key;
    _dict.Add(key, value);
  }

  public bool ContainsKey(string key)
  {
    return _dict.ContainsKey(key);
  }

  public bool Remove(string key)
  {
    return _dict.Remove(key);
  }

  public bool TryGetValue(string key, out ResponseParameter value)
  {
    return _dict.TryGetValue(key, out value!);
  }

  public ResponseParameter this[string key]
  {
    get => _dict[key];
    set
    {
      if (!_dict.ContainsKey(key))
        value.Name = key;

      _dict[key] = value;
    }
  }

  public ICollection<string> Keys => _dict.Keys;

  public ICollection<ResponseParameter> Values => _dict.Values; 

  #endregion
}
