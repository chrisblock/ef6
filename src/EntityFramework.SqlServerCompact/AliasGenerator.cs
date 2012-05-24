namespace System.Data.Entity.SqlServerCompact.SqlGen
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;

    /// <summary>
    /// Generates monotonically increasing names of the form PrefixCounter, where Prefix is an optional prefix string and Counter is the string representation of a monotonically increasing int value that wraps to zero at int.MaxValue
    /// </summary>
    internal sealed class AliasGenerator
    {
        // beyond this size - we recycle the cache and regenerate names
        // this recycling is in place because CTreeGenerator.GenerateNameForVar has prefix of "columnName" which could be unbounded
        private const int MaxPrefixCount = 500;

        // beyond this size per prefix, we don't cache the names (really large queries)
        private const int CacheSize = 250;

        // this caches integer->string so that happens less fequently
        private static readonly string[] _counterNames = new string[CacheSize];

        // We are using a copy-on-write instead of lock-on-read because dictionary is not multi-reader/single-writer safe.
        // safe for frequent multi-thread reading by creating new instances (copy of previous instance) for uncommon writes.
        private static Dictionary<string, string[]> _prefixCounter;

        private int _counter;
        private readonly string _prefix;
        private readonly string[] _cache;

        /// <summary>
        /// Constructs a new AliasGenerator with the specified prefix string
        /// </summary>
        /// <param name="prefix">The prefix string that will appear as the first part of all aliases generated by this AliasGenerator. May be null to indicate that no prefix should be used</param>
        internal AliasGenerator(string prefix)
            : this(prefix, CacheSize)
        {
        }

        internal AliasGenerator(string prefix, int cacheSize)
        {
            _prefix = prefix ?? String.Empty;

            // don't cache all alias, some are truely unique like CommandTree.BindingAliases
            if (0 < cacheSize)
            {
                string[] cache = null;
                Dictionary<string, string[]> updatedCache;
                Dictionary<string, string[]> prefixCounter;
                while ((null == (prefixCounter = _prefixCounter))
                       || !prefixCounter.TryGetValue(prefix, out _cache))
                {
                    if (null == cache)
                    {
                        // we need to create an instance, but it a different thread may win
                        cache = new string[cacheSize];
                    }

                    // grow the cache for prefixes
                    // We are using a copy-on-write instead of lock-on-read because dictionary is not multi-reader/single-writer safe.
                    //     a)Create a larger dictionary
                    //     b) Copy references from previous dictionary
                    //     c) If previous dictionary changed references, repeat from (a)
                    //     d) We now know the individual cache
                    var capacity = 1 + ((null != prefixCounter) ? prefixCounter.Count : 0);
                    updatedCache = new Dictionary<string, string[]>(capacity);
                    if ((null != prefixCounter)
                        && (capacity < MaxPrefixCount))
                    {
                        foreach (var entry in prefixCounter)
                        {
                            updatedCache.Add(entry.Key, entry.Value);
                        }
                    }
                    updatedCache.Add(prefix, cache);
                    Interlocked.CompareExchange(ref _prefixCounter, updatedCache, prefixCounter);
                }
            }
        }

        /// <summary>
        /// Generates the next alias and increments the Counter.
        /// </summary>
        /// <returns>The generated alias</returns>
        internal string Next()
        {
            _counter = Math.Max(unchecked(1 + _counter), 0);
            return GetName(_counter);
        }

        /// <summary>
        /// Generates the alias for the index.
        /// </summary>
        /// <param name="index">index to generate the alias for</param>
        /// <returns>The generated alias</returns>
        internal string GetName(int index)
        {
            string name;
            if ((null == _cache)
                || unchecked((uint)_cache.Length <= (uint)index))
            {
                // names are not cached beyond a particlar size
                name = String.Concat(_prefix, index.ToString(CultureInfo.InvariantCulture));
            }
            else if (null == (name = _cache[index]))
            {
                // name has not been generated and cached yet
                if (unchecked((uint)_counterNames.Length <= (uint)index))
                {
                    // integer->string are not cached beyond a particular size
                    name = index.ToString(CultureInfo.InvariantCulture);
                }
                else if (null == (name = _counterNames[index]))
                {
                    // generate and cache the integer->string
                    _counterNames[index] = name = index.ToString(CultureInfo.InvariantCulture);
                }
                // generate and cache the prefix+integer
                _cache[index] = name = String.Concat(_prefix, name);
            }
            return name;
        }
    }
}
