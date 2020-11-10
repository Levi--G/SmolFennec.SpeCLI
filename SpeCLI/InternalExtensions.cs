using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace SpeCLI
{
    internal static class InternalExtensions
    {
        internal static Exception WithData(this Exception ex, object key, object value)
        {
            ex.Data.Add(key, value);
            return ex;
        }

        internal static object GetDefault(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        internal static IEnumerable<object> OfType(this IEnumerable<object> source, Type type)
        {
            foreach (var item in source)
            {
                if (type.IsAssignableFrom(item.GetType()))
                {
                    yield return item;
                }
            }
        }
        internal static async IAsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> @this, [EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            while (await @this.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (@this.TryRead(out T item))
                {
                    yield return item;
                }
            }
        }
    }
}
