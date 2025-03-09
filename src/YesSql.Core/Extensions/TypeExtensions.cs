using System;

namespace YesSql.Extensions
{
    public static class TypeExtensions
    {
        public static bool HasAttribute<TAttribute>(this Type type)
            where TAttribute : Attribute =>
            type.GetCustomAttributes(typeof(TAttribute), true).Length != 0;
    }
}
