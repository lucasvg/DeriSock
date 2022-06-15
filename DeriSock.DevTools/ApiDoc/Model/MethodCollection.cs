namespace DeriSock.DevTools.ApiDoc.Model;

using System.Collections;
using System.Collections.Generic;

public class MethodCollection : IDictionary<string, Method>
{
  private readonly IDictionary<string, Method> _dict;

  public MethodCollection()
  {
    _dict = new Dictionary<string, Method>();
  }

  public MethodCollection(int capacity)
  {
    _dict = new Dictionary<string, Method>(capacity);
  }

  #region Dictionary

  public IEnumerator<KeyValuePair<string, Method>> GetEnumerator()
  {
    return _dict.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return ((IEnumerable)_dict).GetEnumerator();
  }

  public void Add(KeyValuePair<string, Method> item)
  {
    item.Value.Name = item.Key;
    _dict.Add(item);
  }

  public void Clear()
  {
    _dict.Clear();
  }

  public bool Contains(KeyValuePair<string, Method> item)
  {
    return _dict.Contains(item);
  }

  public void CopyTo(KeyValuePair<string, Method>[] array, int arrayIndex)
  {
    _dict.CopyTo(array, arrayIndex);
  }

  public bool Remove(KeyValuePair<string, Method> item)
  {
    return _dict.Remove(item);
  }

  public int Count => _dict.Count;

  public bool IsReadOnly => _dict.IsReadOnly;

  public void Add(string key, Method value)
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

  public bool TryGetValue(string key, out Method value)
  {
    return _dict.TryGetValue(key, out value!);
  }

  public Method this[string key]
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

  public ICollection<Method> Values => _dict.Values; 

  #endregion
}
