using System.Collections;
using System.Collections.Generic;
using NLua;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class DeconstructHelper
{
    public static void Deconstruct<TK, TV>(this KeyValuePair<TK, TV> pair, out TK key, out TV value)
    {
        key = pair.Key;
        value = pair.Value;
    }

    public static PackDictionaryEnumerable Items(this LuaTable table)
    {
        return new PackDictionaryEnumerable(table);
    }

    public static PackCollectionEnum Enum(this ICollection collection)
    {
        return new PackCollectionEnum(collection);
    }
}

public class PackDictionaryEnumerable : IEnumerable<KeyValuePair<object, object>>
{
    public readonly LuaTable Table;

    public PackDictionaryEnumerable(LuaTable table)
    {
        Table = table;
    }

    public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
    {
        return new PackDictionaryEnumerator(Table.GetEnumerator());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public class PackDictionaryEnumerator : IEnumerator<KeyValuePair<object, object>>
    {
        public readonly IDictionaryEnumerator DictionaryEnumerator;

        public PackDictionaryEnumerator(IDictionaryEnumerator dictionaryEnumerator)
        {
            DictionaryEnumerator = dictionaryEnumerator;
        }

        public bool MoveNext()
        {
            return DictionaryEnumerator.MoveNext();
        }

        public void Reset()
        {
            DictionaryEnumerator.Reset();
        }

        object IEnumerator.Current => Current;

        public KeyValuePair<object, object> Current => new(DictionaryEnumerator.Key, DictionaryEnumerator.Value);

        public void Dispose()
        {
        }
    }
}

public class PackCollectionEnum:IEnumerable<object?>
{
    private readonly ICollection Collection;

    public PackCollectionEnum(ICollection collection)
    {
        Collection = collection;
    }

    IEnumerator<object?> IEnumerable<object?>.GetEnumerator()
    {
        var enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }

    public IEnumerator GetEnumerator()
    {
        return Collection.GetEnumerator();
    }
}