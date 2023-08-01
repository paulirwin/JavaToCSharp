using System;

namespace JavaToCSharpGui;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class ViewAttribute : Attribute
{
    public object Context { get; set; }

    public Type ViewType { get; private set; }

    public ViewAttribute(Type viewType)
    {
        ViewType = viewType;
    }
}
