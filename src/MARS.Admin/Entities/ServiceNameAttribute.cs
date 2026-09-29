namespace MARS.Admin.Entities;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
