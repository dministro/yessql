using System;

namespace YesSql.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RelationContainerAttribute : Attribute
    {
    }
}
