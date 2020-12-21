using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

namespace SpeCLI.Extensions
{
    public static class SpeCLIExtensions
    {
        public static Exception WithData(this Exception ex, object key, object value)
        {
            ex.Data.Add(key, value);
            return ex;
        }

        public static object GetDefault(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static IEnumerable<object> OfType(this IEnumerable<object> source, Type type)
        {
            foreach (var item in source)
            {
                if (type.IsAssignableFrom(item.GetType()))
                {
                    yield return item;
                }
            }
        }

        public static async IAsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> @this, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (await @this.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (@this.TryRead(out T item))
                {
                    yield return item;
                }
            }
        }

        public static T GetCustomAttribute<T>(this ICustomAttributeProvider provider, bool inherit = false)
        {
            return provider.GetCustomAttributes(typeof(T), inherit).Cast<T>().FirstOrDefault();
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this ICustomAttributeProvider provider, bool inherit = false)
        {
            return provider.GetCustomAttributes(typeof(T), inherit).Cast<T>();
        }

        public static Type GetReturnType(this MemberInfo info)
        {
            if (info is PropertyInfo p)
            {
                return p.PropertyType;
            }
            if (info is MethodInfo m)
            {
                return m.ReturnType;
            }
            if (info is Type t)
            {
                return t;
            }
            if (info is FieldInfo f)
            {
                return f.FieldType;
            }
            throw new Exception("non supported member");
        }
    }
}