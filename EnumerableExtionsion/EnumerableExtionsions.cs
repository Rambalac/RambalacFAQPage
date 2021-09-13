using System.Collections.Generic;
using System.Linq;

namespace EnumerableExtionsions
{
    public static class EnumerableExtionsions
    {
        public static TValue GetFirstOf<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<TKey> keys)
        {
            foreach (var key in keys.Where(k => k != null))
            {
                if (dict.TryGetValue(key, out var val))
                {
                    return val;
                }
            }

            return default;
        }
    }
}
