namespace MARS.Admin.Application.Services.ServiceManager;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
