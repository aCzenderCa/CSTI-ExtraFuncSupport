using System.Collections.Generic;
using JetBrains.Annotations;

namespace ChatTreeLoader.Util
{
    public static class SafeRemoveUtil
    {
        public static void SafeRemove<TKey, TVal>(this Dictionary<TKey, TVal> dictionary, [CanBeNull] TKey key)
        {
            if (key == null) return;
            dictionary.Remove(key);
        }
    }
}