using System;
using System.Reflection;

namespace YesSql.Extensions
{
    public static class PropertyInfoAttributes
    {
        public static bool HasAttribute<TAttribute>(this PropertyInfo property)
            where TAttribute : Attribute =>
            property.GetCustomAttribute<TAttribute>() is not null;
    }
}
