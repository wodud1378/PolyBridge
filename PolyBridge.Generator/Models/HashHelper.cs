using System.Collections;

namespace PolyBridge.Generator.Models
{
    // Polyfill for HashCode.Combine, unavailable on netstandard2.0
    internal static class HashHelper
    {
        internal static int Combine(params object[] values)
        {
            unchecked
            {
                var hash = 0;
                foreach (var value in values)
                {
                    if (value is IEnumerable enumerable and not string)
                    {
                        foreach (var item in enumerable)
                            hash = (hash * 397) ^ (item?.GetHashCode() ?? 0);
                    }
                    else
                    {
                        hash = (hash * 397) ^ (value?.GetHashCode() ?? 0);
                    }
                }

                return hash;
            }
        }
    }
}
