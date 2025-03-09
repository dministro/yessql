using System;

namespace YesSql.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ReferencedPropertyAttribute : Attribute
    {
        public string Collection { get; }

        public ReferencedPropertyAttribute(string collection = null) =>
            Collection = collection;
    }
}
