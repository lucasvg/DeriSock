namespace DeriSock.DevTools.CodeDom;

using System;

public struct ManagedTypeInfo
{
  public Type Type;
  public bool IsArray;
  public bool IsNullable;

  public ManagedTypeInfo(Type type, bool isArray, bool isNullable)
  {
    Type = type;
    IsArray = isArray;
    IsNullable = isNullable;
  }
}
