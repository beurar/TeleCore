using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace TeleCore;

public class OrderedDictionary<TKey, TValue> : OrderedDictionary
{
    //
    public ICollection<TKey> Keys => (ICollection<TKey>) base.Keys;
    public ICollection<TValue> Values => (ICollection<TValue>) base.Keys;
    
    public new TValue this[int index]
    {
        get => (TValue)base[index];
        set => base[index] = value;
    }
    
    public TValue this[TKey key]
    {
        get => (TValue)base[key];
        set => base[key] = value;
    }
        
    public void Add(TKey key, TValue value)
    {
        base.Add(key, value);
    }

    public void Remove(TKey key)
    {
        base.Remove(key);
    }
}