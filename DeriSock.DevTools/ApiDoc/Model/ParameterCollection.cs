namespace DeriSock.DevTools.ApiDoc.Model;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using JetBrains.Annotations;

public class ParameterCollection : IDictionary<string, Parameter>
{
  private readonly IDictionary<string, Parameter> _dict;

  [UsedImplicitly]
  public ParameterCollection()
  {
    _dict = new Dictionary<string, Parameter>();
  }

  public ParameterCollection(int capacity)
  {
    _dict = new Dictionary<string, Parameter>(capacity);
  }

  public void Visit(IDocumentationNode? parent, Action<IDocumentationNode?, IDocumentationNode> visitor)
  {
    foreach (var kvp in this) {
      visitor(parent, kvp.Value);
      kvp.Value.ObjectArrayParams?.Visit(kvp.Value, visitor);
    }
  }

  [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
  public override int GetHashCode()
  {
    var hashCodes = Keys.Select(key => HashCode.Combine(key, this[key])).ToArray();
    return hashCodes.Aggregate(hashCodes.Length, (current, code) => unchecked(current * 314159 + code));
  }

  #region Dictionary

  public IEnumerator<KeyValuePair<string, Parameter>> GetEnumerator()
  {
    return _dict.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return ((IEnumerable)_dict).GetEnumerator();
  }

  public void Add(KeyValuePair<string, Parameter> item)
  {
    item.Value.Name = item.Key;
    _dict.Add(item);
  }

  public void Clear()
  {
    _dict.Clear();
  }

  public bool Contains(KeyValuePair<string, Parameter> item)
  {
    return _dict.Contains(item);
  }

  public void CopyTo(KeyValuePair<string, Parameter>[] array, int arrayIndex)
  {
    _dict.CopyTo(array, arrayIndex);
  }

  public bool Remove(KeyValuePair<string, Parameter> item)
  {
    return _dict.Remove(item);
  }

  public int Count => _dict.Count;

  public bool IsReadOnly => _dict.IsReadOnly;

  public void Add(string key, Parameter value)
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

  public bool TryGetValue(string key, out Parameter value)
  {
    return _dict.TryGetValue(key, out value!);
  }

  public Parameter this[string key]
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

  public ICollection<Parameter> Values => _dict.Values;

  #endregion
}
