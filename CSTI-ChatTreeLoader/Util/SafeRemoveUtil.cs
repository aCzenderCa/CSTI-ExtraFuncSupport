using System.Collections.Generic;
using JetBrains.Annotations;

namespace ChatTreeLoader.Util
{
    public static class SafeRemoveUtil
    {
        public static void SafeRemove<TKey, TVal>(this Dictionary<TKey, TVal> dictionary, [CanBeNull] TKey key)
        {
            if (key == null) return;
            if (!dictionary.ContainsKey(key)) return;
            dictionary.Remove(key);
        }
    }
}