using System.Collections.Generic;

namespace Credfeto.Package.Update.Helpers;

internal static class EnumerableExtensions
{
    public static IEnumerable<T> RemoveNulls<T>(this IEnumerable<T?> source)
        where T : notnull
    {
        foreach (T? item in source)
        {
            if (item == null)
            {
                continue;
            }

            yield return item;
        }
    }
}